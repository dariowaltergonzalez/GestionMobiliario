# Lógica de negocio — GestionInmobiliaria

Documento vivo con las decisiones de diseño importantes, organizado por tema. Se escribe durante
el desarrollo (no al final) porque varias de estas decisiones no son obvias y se pierden fácil.

---

## CONTRATO

Ver `docs/modulo-contratos.md` para el detalle completo campo por campo. Resumen de las reglas clave:

- Locador/Locatario/Garante se guardan como **snapshot inmutable** en el Contrato (no cambian si la
  ficha de Propietario/Inquilino se actualiza después). `PropietarioRefId`/`InquilinoRefId` son
  `int?` sin FK real, solo para vincular a una ficha real cuando existe.
- Un contrato solo se puede **editar** (`PUT /api/contratos/{id}`) mientras está en **Borrador**.
  Una vez Vigente, sus datos quedan congelados — no hay forma de modificar nombre, monto base,
  fechas, etc. de un contrato activo.
- Las únicas dos formas en que un contrato Vigente cambia son: el monto vía **ajuste de cuota**, y
  el **estado** (Vigente → Finalizado/Rescindido/Anulado).
- Las cláusulas del contrato (plantilla en `/dashboard/clausulas-contrato`) **no se congelan por
  contrato** — se leen en vivo de la plantilla compartida cada vez que se genera un PDF. Si se edita
  la plantilla después de que un contrato ya es Vigente, un PDF regenerado más tarde puede diferir
  del que se mandó por email en su momento.
- El PDF de contrato que genera el sistema **no tiene firma** — es el borrador/comprobante con los
  datos completados. El contrato válido legalmente es el que se imprime y firman ambas partes fuera
  del sistema (no hay firma digital integrada).
- Hasta 2026-08-07 no había ninguna pantalla que mostrara `ComisionLocadorPorcentaje`/`Monto` ni
  `ComisionLocatarioPorcentaje`/`Monto` fuera del formulario de edición (que solo existe en
  Borrador). Se agregó un botón "Ver detalle" (siempre visible, cualquier estado) que abre
  `DetalleContratoModal.tsx`, de solo lectura, con estos datos y el resto de la info del contrato.

### Comisión Locador vs. Comisión Locatario — no son lo mismo

Dos campos con nombres parecidos pero **conceptos de negocio distintos**:

- **`ComisionLocador`** (al propietario) = comisión por **administración de cobros**, **mensual y
  recurrente**. Se descuenta de cada cuota cobrada — es la que alimenta `Liquidacion` (ver sección
  LIQUIDACIÓN). En el detalle del contrato se muestra también el "monto total teórico" = la suma de
  esa comisión sobre **todas** las cuotas del contrato (usando `Pago.MontoEsperado` de cada una, así
  refleja aumentos ya aplicados).
- **`ComisionLocatario`** (al inquilino) = **honorarios de gestión del contrato**, **pago único** al
  firmar (no confundir con el depósito de garantía, ni con el seguro de caución si el inquilino lo
  usa en vez de garante — son conceptos completamente distintos, ninguno de los dos existe como
  campo en el sistema hoy). No es recurrente, así que en el detalle se muestra el monto calculado
  **una sola vez** (`MontoBase × Porcentaje`, o el monto fijo tal cual), sin multiplicar por cuotas.
- Por eso **`Liquidacion` solo usa la comisión del Locador** — la del Locatario no tiene ningún
  cálculo automático conectado todavía (sería, en todo caso, un cobro único al firmar el contrato,
  no algo que pase por el flujo de Pagos/Liquidaciones).

---

## NOTIFICACIONES

Sistema de emails automáticos, iniciado 2026-08-04/06. Objetivo: avisar por email en ciertos eventos
del negocio, pero permitiendo excepciones por persona (no una regla global on/off por tarea).

### Diseño

- **La preferencia vive en la persona (`Propietario`/`Inquilino`), no en el `Contrato`.** Un campo
  `Notificaciones` (string, JSON) por entidad: `{"NuevoContrato": true, "AvisoCobro": false}`. Se
  descartaron: columnas booleanas fijas (rígido) y una tabla genérica de preferencias (más trabajo
  del necesario).
- **Catálogo de temas fijo en código, por módulo**, en `TemasNotificacion`
  (`GestionInmobiliaria.Aplicacion/DTOs/NotificacionDto.cs`). El admin no puede inventar temas —
  elige de un combo que un desarrollador va llenando a medida que se agregan eventos nuevos. El
  combo del frontend lo carga dinámicamente (`GET /api/{propietarios|inquilinos}/temas-notificacion`),
  así que agregar un tema nuevo no requiere tocar el frontend.
- **Opt-in puro, sin default en el código.** Si un tema no está configurado para una persona, no se
  envía nada — no hay ningún `else` que decida un default. Si mañana se quiere un preset "todo en
  true", el default va a vivir en el dato (filas precreadas), nunca en una condición hardcodeada.
- **El `Activo` de la persona también gatea el envío** (además de la preferencia): dar de baja a un
  inquilino/propietario equivale a "no le mandes nada más", sin importar lo que tenga configurado.

### Servicio (`INotificacionService` / `NotificacionService`)

Un solo punto de entrada para cualquier evento, presente o futuro:

```
NotificarAsync(destinatario: INotificable, tema: string, asunto, cuerpo, contexto, adjuntos?)
```

- `INotificable` (`Activo`, `Email`, `Notificaciones`) lo implementan `Propietario` e `Inquilino`.
- Chequea `Activo` + `Notificaciones[tema]`, y si corresponde, llama a `IEmailService`.
- **Auditoría centralizada acá adentro** (no en cada controller): cada intento queda en
  `AuditLogs` con `Action` = `ENVIADO` / `OMITIDO` (con motivo) / `ERROR`, visible en
  `/dashboard/auditoria`. Por eso cada evento nuevo que se conecta no tiene que reimplementar
  logging ni manejo de errores.
- Los envíos corren en `Task.Run` fire-and-forget (no bloquean la respuesta HTTP). Como ese código
  corre sin `HttpContext`, no se puede usar `ITenantService` para filtrar por tenant — hay que leer
  `ConfiguracionEmpresa`/`Propietarios`/`Inquilinos` con `IgnoreQueryFilters()` + filtro manual por
  el `TenantId` ya capturado antes de entrar al `Task.Run` (mismo patrón que ya usaba
  `SmtpEmailService` para `ConfiguracionEmpresa`).

### Catálogo de temas y dónde se disparan

| Tema | Propietario | Inquilino | Se dispara en | Adjunto |
|---|---|---|---|---|
| `NuevoContrato` | ✓ | ✓ | Contrato pasa a Vigente (`Create` o `TransicionEstado`) | PDF del contrato |
| `AvisoAumento` | ✓ | ✓ | `POST /api/contratos/{id}/ajustes` | — |
| `AvisoCobro` / `ReciboPago` | ✓ (AvisoCobro) | ✓ (ReciboPago) | `PUT /api/pagos/{contratoId}/pagos/{pagoId}` con Estado=Pagado | PDF del recibo |
| `CambioEstadoContrato` | ✓ | ✓ | `TransicionEstado` a Finalizado/Rescindido/Anulado | — |

Todos viven en `ContratosController` salvo el de cobro/recibo, que vive en `PagosController`.

---

## LEADS

- `Lead` y `Inquilino` son entidades **separadas a propósito**: el Lead es el historial comercial
  (origen, quién lo contactó, cuándo se convirtió), el Inquilino es la ficha operativa que se usa en
  contratos y notificaciones. No hay FK entre ambos, solo una nota de texto ("Convertido desde Lead")
  que queda en el Inquilino nuevo.
- De los 5 estados (Nuevo/Contactado/Interesado/Convertido/Descartado), **solo `Convertido` dispara
  algo**: al guardar esa transición, se abre el alta de Inquilino **precargada** (nombre, apellido,
  email, teléfono) pero sin crear nada solo — el usuario completa el resto (DNI, dirección, garante)
  y confirma. No es 100% automático porque el Lead no tiene datos suficientes para un Inquilino real.
- **Un Lead Convertido queda congelado** (`LeadsController.Update` lo rechaza, botón Editar oculto en
  el frontend) — mismo criterio que Contrato: una vez que algo llega a su estado definitivo, las
  correcciones se hacen en la ficha operativa (Inquilino), no en el registro de origen.
- El Lead **no se borra ni se inactiva** al convertirse — sigue existiendo como historial. El filtro
  de "activos" para seguimiento comercial ya lo excluye por `Estado` (no por el flag `Activo`, que es
  para borrado lógico real).

---

## LIQUIDACIÓN

Lo que la inmobiliaria le tiene que transferir al propietario después de cobrar una cuota,
descontando su comisión de administración. Iniciado 2026-08-07.

- **Se genera automáticamente**, no hay botón para crearla a mano. Cuando un `Pago` pasa a
  `Pagado` (`PagosController.UpdatePago`), si el contrato tiene `AdministracionCobros = true` **y**
  tiene cargado `ComisionLocadorPorcentaje` o `ComisionLocadorMonto`, se crea una `Liquidacion`:
  `MontoALiquidar = MontoCobrado - Comisión` (la comisión es el monto fijo si está cargado, si no
  el porcentaje sobre lo cobrado). Si no hay comisión configurada, no se genera nada — no tiene
  sentido "liquidar" si la inmobiliaria no se queda con nada.
- Es **idempotente**: antes de crear, chequea si ya existe una `Liquidacion` para ese `PagoId`
  (índice único), así que no se duplica si por algún motivo se vuelve a procesar el mismo pago.
- Cada generación queda logueada (`ILogger`, no auditoría en `AuditLogs` — es un dato financiero
  interno, no una notificación a alguien externo).
- Pantalla nueva `/dashboard/liquidaciones`, mismo patrón que Pagos: estado `Pendiente` → un admin
  confirma que transfirió la plata → `Liquidado` (con fecha y observaciones opcionales, ej. número
  de transferencia).
- **Solo usa `ComisionLocadorPorcentaje`/`Monto`** — ver sección CONTRATO ("Comisión Locador vs.
  Comisión Locatario") para la diferencia entre las dos comisiones y por qué solo una entra acá.
- No está conectado al sistema de Notificaciones — no se le avisa al propietario por email cuando
  se genera o se liquida. Si se quiere en el futuro, sería un tema nuevo del catálogo
  (`LiquidacionRealizada` o similar).
