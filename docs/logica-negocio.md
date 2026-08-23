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
- No se permite crear/activar un contrato Vigente para una propiedad que ya tiene otro contrato
  Vigente (bug real encontrado 2026-08-09 probando Gastos: una propiedad terminó con 2 contratos
  Vigentes al mismo tiempo) — hay que Finalizar/Rescindir el anterior primero.
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
| `AvisoLiquidacion` | ✓ | — | `POST /api/liquidaciones/{id}/abonos` (cada abono, no solo al completar) | — |
| `AvisoVencimientoProximo` | — | ✓ | `RecordatorioVencimientoService` (chequeo diario en background) | — |
| `AvisoGastoPendiente` | — | ✓ | `POST /api/gastos` con `Responsable=Inquilino` y `ContratoId` cargado | — |

Todos viven en `ContratosController`, salvo cobro/recibo (`PagosController`), liquidación
(`LiquidacionesController`), gasto pendiente (`GastosController`) y vencimiento próximo
(`RecordatorioVencimientoService`, es el único que no se dispara desde un controller — ver más abajo).

### `AvisoVencimientoProximo` — el primer evento que no nace de una acción (2026-08-08)

Los otros 4 temas se disparan porque alguien hizo algo (cobrar, ajustar, cambiar estado). Este es
distinto: hay que avisar **antes** de que pase nada, así que necesita algo revisando el calendario
por su cuenta. Es el primer `BackgroundService` del proyecto (`RecordatorioVencimientoService`,
`GestionInmobiliaria.Infraestructura/Services/`), registrado con
`builder.Services.AddHostedService<...>()` en `Program.cs`.

- **Regla de negocio**: un aviso 7 días antes del vencimiento y otro 1 día antes. Nada más — nada si
  la cuota ya venció (no se manda un aluvión de "está atrasada" todos los días).
- **Vencimiento de una cuota** = `Pago.Periodo` (mes/año) + `Contrato.DiaVencimientoPago` (día),
  clampeado al último día del mes si el contrato tiene cargado un día que ese mes no tiene (ej: 31 en
  febrero). Si el contrato no tiene `DiaVencimientoPago` cargado, esa cuota queda afuera del sistema
  de avisos — no hay nada que calcular.
- **Por qué hay 2 campos nuevos en `Pago`** (`AvisoVencimiento7DiasEnviado`, `AvisoVencimiento1DiaEnviado`)
  en vez de comparar "¿hoy es exactamente el día -7?": así es robusto si el servidor estuvo caído
  justo ese día — el chequeo es "¿faltan 7 días **o menos** y todavía no se mandó?", no "¿faltan
  exactamente 7?". Evita perder el aviso para siempre por una caída puntual, sin arriesgar mandar el
  mismo aviso dos veces (el flag lo evita).
- El chequeo corre **como mínimo una vez por día** (`Task.Delay` de 24hs entre corridas) — no importa
  si corre de más (ej: el server se reinicia varias veces en el día), los flags hacen que sea
  idempotente: si ya se mandó, no se vuelve a mandar aunque se re-evalúe.
- **No hay tenant "activo"** en este contexto (no hay request HTTP corriendo) — a diferencia de los
  otros 4 eventos, acá hay que recorrer **todos los Tenants activos** y filtrar cada consulta a mano
  por `TenantId` (`IgnoreQueryFilters()` en cada query, no solo en la búsqueda del destinatario).

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
  sentido "liquidar" si la inmobiliaria no se queda con nada. Desde que existe el módulo de GASTOS,
  también se resta `MontoGastos` (ver esa sección) — la fórmula completa es
  `MontoALiquidar = MontoCobrado - MontoComision - MontoGastos`.
- **Desglose visible en la grilla (2026-08-10)**: probando el módulo surgió que el número de
  "A liquidar" no se entendía a simple vista cuando había comisión y/o gastos descontados. Se agregó
  una fila expandible (flecha ▼, solo aparece si hay algo que explicar) en `/dashboard/liquidaciones`
  que muestra Cobrado → Comisión → cada Gasto descontado (categoría + descripción) → A liquidar. El
  backend expone esto en `LiquidacionDto.MontoGastos` y `LiquidacionDto.Gastos` (requiere
  `.Include(l => l.Gastos)` en `LiquidacionRepository.QueryConIncludes`).
- Es **idempotente**: antes de crear, chequea si ya existe una `Liquidacion` para ese `PagoId`
  (índice único), así que no se duplica si por algún motivo se vuelve a procesar el mismo pago.
- Cada generación queda logueada (`ILogger`, no auditoría en `AuditLogs` — es un dato financiero
  interno, no una notificación a alguien externo).
- Pantalla nueva `/dashboard/liquidaciones`.
- **Solo usa `ComisionLocadorPorcentaje`/`Monto`** — ver sección CONTRATO ("Comisión Locador vs.
  Comisión Locatario") para la diferencia entre las dos comisiones y por qué solo una entra acá.
- **Notifica al propietario por email en cada abono** (tema `AvisoLiquidacion`, ver tabla en
  NOTIFICACIONES) — no solo cuando se completa el total. El mensaje no afirma si la liquidación
  queda saldada o no (evita contradecirse si después se corrige/borra un abono), solo confirma el
  movimiento puntual: monto, propiedad, contrato, período, medio, y los datos de la transferencia
  que se hayan cargado (CBU, entidad, N° de operación). Se dispara en
  `LiquidacionesController.AgregarAbono`, nunca al editar ni al eliminar un abono.

### Abonos parciales (2026-08-08, rediseño)

Una `Liquidacion` no se paga necesariamente de una sola vez, así que **no tiene sus propios campos
de "estado liquidado"** — tiene una lista de `LiquidacionAbono` (mismo patrón que `Pago`/`PagoDetalle`:
una cabecera con el total, varios "detalles" reales que se van cargando por separado, cada uno con
`Medio` — mismo enum `MedioPago` que Pagos —, `CbuCvuDestino`, `EntidadDestino`, `NumeroOperacion`,
`Monto`, `Fecha`, `Observaciones`).

- **`Estado` y `FechaLiquidacion` se recalculan siempre a partir de la suma de abonos activos**,
  nunca se setean a mano: `Pendiente` (suma = 0) → `Parcial` (suma > 0 pero < total) → `Liquidado`
  (suma ≥ total, y ahí `FechaLiquidacion` = la fecha del último abono). Esto pasa en
  `LiquidacionRepository.AplicarEstadoAsync`, se llama después de agregar, editar o eliminar
  cualquier abono.
- **Bug real que se dio acá (2026-08-08) y cómo se resolvió**: la primera versión calculaba la suma
  usando la colección `liquidacion.Abonos` cargada con `.Include(l => l.Abonos)`, asumiendo que el
  filtro global "solo activos" se aplicaba automáticamente a través del `Include`. En la práctica no
  fue confiable — un abono borrado seguía contando en el cálculo, dejando una Liquidación marcada
  como Liquidado cuando en realidad estaba Parcial. Se corrigió calculando la suma con una consulta
  directa contra `LiquidacionAbonos` (`SumAsync`/`MaxAsync` con `Where` explícito), sin pasar por
  `Include` para nada que dispare un cálculo. Regla para el futuro: si un cálculo depende de "solo
  las filas activas", no confiar en que el filtro global se propague solo a través de una navegación
  — consultarlo explícito.
- **Corregir un dato mal cargado** = editar el abono (`PUT /{id}/abonos/{abonoId}`), no hay que
  tocar la Liquidacion en sí.
- **"Me equivoqué de comprobante, esto no era"** = eliminar ese abono puntual
  (`DELETE /{id}/abonos/{abonoId}`, baja lógica) — el estado se recalcula solo y vuelve para atrás
  (de Liquidado a Parcial, o a Pendiente si era el único).
- **Pagar en varias partes** = agregar varios abonos contra la misma Liquidacion hasta cubrir el
  total. Cada abono se valida contra lo que falta (`MontoALiquidar - suma de abonos activos`), no se
  puede cargar de más.
- **Eliminar la Liquidacion entera** solo se permite si está `Pendiente` **y** no tiene ningún abono
  **activo** (si tuvo abonos que ya se borraron, no cuenta) — si tiene abonos activos, hay que
  borrarlos primero, uno por uno. Esto es a propósito: evita borrar de un tirón un registro que
  todavía tiene plata real transferida asociada.

---

## GASTOS

Gastos de una propiedad (reparaciones, impuestos, expensas, seguros...) que pueden ser a cargo del
propietario o del inquilino. Iniciado 2026-08-09, primera funcionalidad construida a partir de la
comparación con Barreeo — a propósito antes que el Portal de autoservicio, para que cuando ese portal
exista ya tenga datos reales que mostrar.

### Diseño

- **Se asocia a `Propiedad` (obligatorio) y a `Contrato` (opcional)**. La propiedad es el dato que no
  cambia nunca; el contrato sí puede cambiar a lo largo del tiempo (varios inquilinos distintos), así
  que queda como referencia de contexto ("qué contrato estaba vigente cuando se cargó"), no como
  dueño real del gasto.
- **`Responsable` (Propietario/Inquilino) se elige por gasto, no es una regla global** — ambos casos
  existen en la práctica (una reparación estructural es del propietario, una rotura por uso normal
  puede recaer en el inquilino según el contrato).
- **`Categoria` es un enum fijo en código** (`Reparacion`, `Impuesto`, `Expensas`, `Seguro`, `Otro`),
  no texto libre ni tabla configurable — mismo criterio que otros catálogos chicos del sistema.
- **Gasto a cargo del Inquilino: se investigó cómo lo manejan otras plataformas** (Rentger, Rentila,
  Yardi Breeze, EnergyCAP, SnapInspect, Barreeo) antes de decidir el diseño. La práctica estándar de
  la industria es mostrar estos gastos como **línea aparte, nunca fusionados dentro del monto de la
  cuota** — por transparencia y porque un monto de alquiler que cambia sin aviso genera reclamos. Por
  eso acá **`Gasto` con `Responsable = Inquilino` NO modifica `Pago.MontoEsperado` para nada** —
  queda con `Estado = Pendiente` hasta que alguien lo marca `Resuelto` a mano
  (`PUT /api/gastos/{id}/resolver`) cuando la inmobiliaria lo cobra por fuera del circuito de cuotas.
  Esto es más simple y más seguro que auto-sumarlo a una cuota (evita el riesgo de alterar un monto
  que después hay que explicarle al inquilino).
- **Cobro al Inquilino: notificación + registro de cómo se cobró (2026-08-10)**. Probando el módulo
  surgió la pregunta obvia: si el sistema no hace nada automático con el gasto a cargo del inquilino,
  ¿en algún momento se entera y se le cobra? Se cerró parcialmente esa brecha (sigue sin auto-cobrarse,
  a propósito, ver punto anterior):
  - Al crear un `Gasto` con `Responsable = Inquilino` y `ContratoId` cargado, se dispara el tema
    `AvisoGastoPendiente` (nuevo, solo Inquilino) al inquilino real del contrato (vía
    `Contrato.InquilinoRefId`) — mismo patrón fire-and-forget que el resto de las notificaciones. Si
    el gasto no tiene `ContratoId` (no se sabe a qué inquilino avisar) o el inquilino no tiene la
    preferencia activada, simplemente no se manda nada, como siempre.
  - `PUT /api/gastos/{id}/resolver` ahora pide el detalle completo del cobro, con el mismo nivel que
    "Registrar cobro" en Pagos: **`Medio`** (reusa `MedioPago`), **`Fecha`** editable (antes se
    autoseteaba al momento de tildar Resuelto), y campos condicionales según el medio — `Referencia`
    para Débito/Crédito, `ChequeBanco`/`ChequeNumero`/`ChequeFechaVencimiento` para Cheque (mismos
    campos que `PagoDetalle`) — más `Observaciones` libres. Todos estos campos
    (`MedioCobro`, `FechaCobro`, `ReferenciaCobro`, `Cheque*`, `ObservacionesResolucion`) solo tienen
    sentido para gastos a cargo del Inquilino (el descuento al Propietario vía Liquidación no pasa por
    acá, se resuelve solo).
- **Gasto a cargo del Propietario: se descuenta automático de la próxima Liquidación de esa
  propiedad.** Cuando se genera una `Liquidacion` (`PagosController.GenerarLiquidacionSiCorrespondeAsync`,
  ver sección LIQUIDACIÓN), antes de calcular `MontoALiquidar` se buscan los `Gasto` con
  `Responsable = Propietario` y `Estado = Pendiente` de esa `PropiedadId`, se suman en el nuevo campo
  `Liquidacion.MontoGastos`, se restan del monto a liquidar (`MontoALiquidar = MontoCobrado -
  MontoComision - MontoGastos`), y esos gastos quedan `Resuelto` con `LiquidacionId` apuntando a la
  liquidación que los absorbió — quedan trazables, no se pierden en el descuento.
- **Edición/borrado solo mientras está `Pendiente`** — una vez `Resuelto` (sea porque se marcó a mano
  o porque una Liquidación ya lo descontó), el registro queda congelado, mismo criterio que Contrato
  Vigente o Liquidación con abonos: si algo ya impactó en un movimiento de dinero real, no se toca
  retroactivamente.
- `VisibleParaInquilino` es un campo pensado para el futuro Portal de autoservicio (que gastos del
  propietario se le muestren o no al inquilino) — hoy no se usa en ningún lado del sistema todavía,
  se guarda para no tener que migrar de nuevo cuando se construya el portal.

---

## PENDIENTES GENERALES

Lista única de lo que falta, para no depender de la memoria de sesión a sesión. Se va tachando o
sacando a medida que se resuelve, como el resto del documento.

- [ ] **Anulación de cobros — EN ANÁLISIS, sin decidir ni implementar (2026-08-23)**. Hoy no existe
  ninguna forma de anular/deshacer un `Pago` ya marcado `Pagado`. El enum `EstadoPago.Anulado` (4)
  existe pero está **muerto** — se grepeó todo el código y no lo usa nada, ni backend ni frontend
  (mismo caso que `EstadoPago.Atrasado`, que tampoco lo setea nada — ver más abajo). Surgió al
  encontrar que con el checkbox nuevo de punitorio es fácil cobrar mal por error y no hay forma de
  corregirlo.

  **Por qué no es tan simple — importa CUÁNDO se pide anular**, porque entre el cobro y el pedido de
  anulación pueden haber pasado cosas automáticas que "usaron" ese cobro:
  1. **Inmediatamente**: ya se mandaron los emails `AvisoCobro`/`ReciboPago` con el recibo adjunto
     (fire-and-forget, no se pueden desenviar). Si el contrato tiene administración de cobros +
     comisión: ya se generó sola una `Liquidacion` (Pendiente, sin abonos). Si había `Gasto` del
     Propietario Pendientes en la propiedad: ya se descontaron ahí (quedaron Resuelto).
  2. **Unos días después, la Liquidación sigue Pendiente** (nadie le transfirió nada al propietario
     todavía): todavía es "seguro" revertir todo de una — no salió plata real del sistema.
  3. **Después de que la Liquidación tiene abonos** (el propietario ya cobró, parcial o total): acá
     ya no se puede simplemente deshacer. Se investigó cómo lo manejan plataformas reales de property
     management (Rentvine, AppFolio) y el principio contable general — **nunca se reescribe una
     transacción ya liquidada, se corrige hacia adelante con un ajuste** (equivalente a una factura
     rectificativa). Acá eso significa: generar un `Gasto` correctivo a cargo del Propietario que se
     le descuenta de su **próxima** Liquidación, reusando la infraestructura que ya existe para
     Gastos — no hay que inventar un mecanismo nuevo.

  **Hallazgo relacionado, mientras se investigaba esto**: hoy el punitorio cobrado se está liquidando
  **100% al propietario, y la comisión de la inmobiliaria se calcula sobre el total incluyendo el
  punitorio** — porque `GenerarLiquidacionSiCorrespondeAsync` usa `pago.MontoPagado` (que ya incluye
  cuota + punitorio) como `MontoCobrado`. Confirmado en la base con el pago de prueba: Liquidación del
  Pago #2 → `MontoCobrado=462.465,52`, `MontoComision=9.249,31` (2% de todo, punitorio incluido).
  Nadie decidió esto a propósito, salió solo al sumar el punitorio a `MontoPagado`. Es una pregunta
  aparte, pero pesa acá porque cambia cuánto hay que reclamarle al propietario si se anula un cobro
  ya liquidado con punitorio adentro.

  **Preguntas abiertas para decidir**:
  1. ¿Se permite anular en cualquier momento (con lógica distinta según el estado de la Liquidación),
     o hay un punto más allá del cual directamente no se puede anular desde el sistema?
  2. Con la Liquidación ya con abonos: ¿el Gasto correctivo es por el total liquidado al propietario,
     o hay que restarle la comisión que la inmobiliaria ya se quedó (para no reclamarle algo que
     nunca tuvo)?
  3. Si la Liquidación está "Parcial" (una parte transferida, otra no): ¿se ajusta directo la parte
     pendiente de esa misma Liquidación, y solo se genera Gasto correctivo por la parte YA
     transferida?
  4. ¿El punitorio le corresponde al propietario (como pasa hoy sin haberlo decidido) o debería
     quedarse 100% en la inmobiliaria? Cambia toda la cuenta de "cuánto reclamar" al anular.
  5. ¿Motivo obligatorio para anular? (propuesta: sí, mismo criterio que Rescindir/Anular contrato).
  6. ¿Se resetean los flags de aviso de vencimiento (`AvisoVencimiento7Dias/1DiaEnviado`) al volver la
     cuota a Pendiente, o quedan como estaban?

  **A favor, no hay que construir de cero**: el mecanismo de "Gasto a cargo del Propietario
  descontado de la próxima Liquidación" ya existe y andaría igual acá. `LiquidacionRepository.EliminarAsync`
  ya bloquea borrar una Liquidación con abonos activos — mismo criterio que hace falta acá. `Pago` ya
  es `IAuditable`, así que cualquier reversión queda sola en `AuditLogs`.
- [ ] **Probar `AvisoVencimientoProximo` en la práctica** (implementado 2026-08-08, sin probar
  todavía). Difícil de replicar rápido porque depende de fechas de vencimiento reales — el chequeo
  corre una vez por día, así que verificar el circuito completo (7 días antes, 1 día antes, nada si
  ya venció) lleva varios días de prueba real, no algo que se pueda apurar con un botón. Para
  probarlo hace falta un contrato con `DiaVencimientoPago` cargado y una cuota Pendiente cuyo
  vencimiento caiga justo en alguna de esas ventanas.
- [ ] **Autocompletar los datos de la transferencia de una Liquidación a partir de una foto/captura
  del comprobante** (ej: comprobante de Mercado Pago o de un banco). No es viable con OCR tradicional
  porque cada entidad tiene un diseño de comprobante distinto — la vía realista es mandarle la imagen
  a un modelo de IA con visión (ej. API de Claude) pidiendo que devuelva un JSON con los campos
  (monto, fecha, CBU/CVU destino, entidad, número de operación) y precargar el formulario de "Marcar
  liquidado" con eso — **siempre mostrando el resultado para que el usuario lo confirme antes de
  guardar, nunca autocompletar y guardar directo** (es plata). Implica: subida de imagen, integración
  con un servicio de IA externo (API key, costo por imagen), y el prompt de extracción. Depende de
  que existan los campos estructurados (`Medio`, `CbuCvuDestino`, `EntidadDestino`,
  `NumeroOperacion`) — ya están, así que esto queda listo para atacar cuando se priorice.

### Ideas sacadas de investigar la competencia (Barreeo, 2026-08-09)

Barreeo es un competidor enfocado 100% en administración de alquileres (no cubre venta, tasaciones,
leads, agentes como nosotros). Se revisó su sitio para comparar funcionalidades. Prioridad acordada
con el usuario:

- [ ] **Portal de autoservicio para Inquilino y Propietario** (prioridad alta). Hoy el sistema es
  100% panel interno (Admin/Operador/Agente) — no hay ninguna vista liviana pública/semi-pública
  donde el inquilino vea su estado de cuenta (cuánto debe, histórico de pagos, próximo vencimiento)
  o el propietario vea sus liquidaciones, sin loguearse al dashboard completo. Falta definir
  mecanismo de acceso (¿link mágico por email? ¿usuario/contraseña liviano?).
- [x] **Punitorios automáticos por mora** (implementado 2026-08-22 — diseñado 2026-08-11, programado
  y probado el mismo día que se terminó de diseñar). Falta todavía probarlo con una cuota realmente
  atrasada (la única Pendiente hoy vence en el futuro), pero el cálculo y toda la infraestructura ya
  están escritos, compilando y con la parte de la tasa TIM verificada en vivo.

  **Investigación (2026-08-11)**: cómo se manejan los punitorios de alquiler en Argentina hoy —
  - Marco legal: art. 768 CCyCN — la tasa aplicable es 1) lo pactado en el contrato, 2) ley especial,
    3) subsidiariamente la que fije el BCRA. Nuestra propia cláusula SÉPTIMA (plantilla de contrato)
    ya dice *"tasa activa por plazo fijo del Banco de la Nación Argentina"* como texto por defecto.
  - Práctica de mercado: la mayoría pacta una tasa fija (~1% diario / 36% anual) o referencia la tasa
    BNA. Interés **simple** (lineal por día), no compuesto — así calculan los juzgados en estos casos.
  - Competidor (Barreeo): su calculadora pide monto + fecha vencimiento + fecha pago + tasa del
    contrato, la marcan como "orientativa" ("depende de lo que diga el contrato"). En su plataforma
    completa se carga la regla de punitorios por contrato y el sistema calcula solo.
  - Dato nuevo relevante: desde enero 2026 el BCRA publica una tasa pensada específicamente para esto,
    la **TIM (Tasa de Intereses Moratorios)**, vía Resolución 1/2026 — pensada como referencia oficial
    para que los tribunales calculen intereses moratorios (art. 768 CCyCN). Más prolija que perseguir
    la tasa BNA específica, que el banco solo publica en PDF (no hay API limpia para eso).

  **Descubrimiento clave al programar (2026-08-22): la TIM NO es una tasa % periódica, es un ÍNDICE
  ACUMULADO** (viene subiendo desde 1993-06-03, hoy vale ~163.000 — mismo mecanismo que el CER).
  Se usa dividiendo dos valores del índice, nunca "días de atraso × tasa diaria":

  ```
  recargo = MontoAdeudado × (Valor_TIM(fecha_de_cobro) / Valor_TIM(fecha_de_vencimiento) − 1)
  ```

  Esto de hecho **resuelve solo** la duda de "interés simple o compuesto" que quedó abierta el
  2026-08-11 — el BCRA ya lo resolvió del lado de ellos al construir el índice, nosotros solo
  dividimos dos valores. Confirmado en vivo contra `api.bcra.gob.ar/estadisticas/v4.0/monetarias/1197`
  (`idVariable=1197`, descripción *"Tasa de Intereses Moratorios (TIM) CCC, art. 768(c)"*,
  periodicidad diaria, serie completa desde 1993-06-03).

  **Decisiones tomadas con el usuario (2026-08-11)**:
  - Campo nuevo en `Contrato` con un % fijo diario (mismo patrón Porcentaje/Monto que
    `ComisionLocador`) — este sí sería una tasa % simple tradicional, no un índice. **Regla híbrida**:
    si ese % fijo es > 0, se usa ese. Si está en 0/vacío, se usa la fórmula del índice TIM de arriba
    — así los contratos con la cláusula por defecto (que no cargan un % propio) también calculan algo.
  - Cálculo del monto de punitorio: **en vivo**, al mostrar/cobrar la cuota — nunca se guarda un
    acumulado que se desactualiza.

  **Importante — no es un proceso automático ni corre a una hora fija (aclarado 2026-08-22)**: a
  diferencia de `RecordatorioVencimientoService` o `TasaMoratoriaSchedulerService`, el punitorio **no
  tiene ningún `BackgroundService` propio**. `IPunitorioService.CalcularAsync(pago)` se ejecuta
  **cada vez que se pide la lista de Pagos o las cuotas de un contrato** (`GET /api/pagos`,
  `GET /api/contratos/{id}/pagos`) — nunca al registrar un cobro específicamente, y nunca en un
  horario programado. El monto que se ve es siempre "a hoy": si se vuelve a mirar la misma cuota al
  día siguiente, el número cambia solo (un día más de atraso, y la tasa TIM puede haber cambiado). No
  se persiste en ningún lado. Lo único que sí corre 1 vez por día es
  `TasaMoratoriaSchedulerService` — pero ese actualiza la *materia prima* (la tabla `TasasMoratorias`),
  no calcula ningún punitorio; el cálculo en sí lee esa tabla al vuelo cuando alguien pide ver una
  cuota.

  **Infraestructura de la tasa TIM — implementada y probada (2026-08-22)**:
  - Entidad `TasaMoratoria` (`Dominio/Entidades/TasaMoratoria.cs`): `Fecha` (día que reporta el BCRA,
    índice único), `Valor` (`decimal(18,8)`, necesita esa precisión — el índice viene con hasta 4
    decimales sobre una base de 6 dígitos enteros), `Origen` = "BCRA", `FechaConsulta`. Es dato
    **global, no por tenant** (la tasa es la misma para todas las inmobiliarias) — a propósito no
    tiene campo `TenantId` ni `HasQueryFilter`; igual queda auditada en `AuditLogs` por implementar
    `IAuditable` (sin nada especial que armar — era el pedido explícito del usuario).
  - `ITasaMoratoriaService.ActualizarAsync()` (`Infraestructura/Services/TasaMoratoriaService.cs`):
    si la tabla está vacía hace una **carga histórica completa** (pagina de a 3000 registros — es el
    máximo que permite la API del BCRA por request, `"El límite no puede superar los 3000 registros"`;
    la serie completa son ~12.136 valores, entran en 5 páginas); si ya hay datos, solo trae
    `desde = últimaFecha + 1 hasta = hoy`. Deserializa la respuesta de
    `api.bcra.gob.ar/estadisticas/v4.0/monetarias/1197` con DTOs internos (`JsonPropertyName`, la API
    devuelve todo en minúscula: `results`/`detalle`/`fecha`/`valor`). La API funciona por HTTPS normal
    sin problemas de certificado (`AddHttpClient("Bcra", ...)` en `Program.cs`, sin handler especial).
  - **Bug real encontrado probando (2026-08-22) y cómo se resolvió**: el `BackgroundService` arranca
    su primer ciclo apenas levanta la app (no espera las 24hs), así que al probar manualmente justo
    después de un arranque en frío, el disparo manual (`POST /actualizar`) y el ciclo automático
    corrieron al mismo tiempo — los dos vieron la tabla vacía, los dos dispararon la carga histórica
    completa, y chocaron al insertar las mismas fechas (`Cannot insert duplicate key row... IX_TasasMoratorias_Fecha`).
    Se solucionó con un `SemaphoreSlim` **estático** dentro de `TasaMoratoriaService` que serializa
    todas las llamadas a `ActualizarAsync()` dentro del proceso — el segundo llamado simplemente
    espera al primero y después no encuentra nada nuevo para traer. Mismo criterio que ya se aplicó en
    otros lados del sistema: el disparo manual y el automático **son literalmente el mismo método**,
    nunca lógica duplicada, así que alcanzó con proteger ese único método.
  - `TasaMoratoriaSchedulerService` (`BackgroundService`, mismo patrón que
    `RecordatorioVencimientoService`) — llama a `ActualizarAsync()` una vez por día.
  - `TasasMoratoriasController`: `GET /api/tasas-moratorias/ultima` (valor vigente), `GET /api/tasas-moratorias`
    (histórico, filtrable por `desde`/`hasta`), `POST /api/tasas-moratorias/actualizar`
    (`Authorize(Roles="Admin")`, dispara el mismo servicio que el background job).
  - Probado en vivo contra la base real: carga histórica completa insertó los 12.136 valores
    correctos (1993-06-03 a 2026-08-24), y una segunda corrida detectó correctamente "ya está al día"
    sin duplicar nada.

  **Cálculo del punitorio en sí — implementado 2026-08-22**:
  - `Contrato.PunitorioPorcentaje` (`decimal(7,4)`, nullable) — % diario fijo, opcional.
  - `IPunitorioService.CalcularAsync(Pago pago)` (`Infraestructura/Services/PunitorioService.cs`,
    requiere `pago.Contrato` cargado): si `Estado` no es Pendiente/Atrasado devuelve 0; si el contrato
    no tiene `DiaVencimientoPago` cargado devuelve 0 (no hay con qué calcular vencimiento); si la
    cuota no está vencida devuelve 0; si `PunitorioPorcentaje > 0` usa ese % simple diario
    (`Monto × %/100 × díasAtraso`); si no, usa la fórmula del índice TIM de arriba, buscando en
    `TasasMoratorias` el valor **más reciente disponible en o antes de** cada fecha (no exige un
    match exacto, por si algún día falta en la serie). Si no hay ninguna tasa TIM cargada todavía,
    devuelve 0 en vez de inventar un número — nunca "no calcular nada" se confunde con "punitorio
    cero real".
  - Se extrajo `VencimientoCalculator.Calcular(periodo, diaVencimientoPago)`
    (`Dominio/Common/VencimientoCalculator.cs`) de adentro de `RecordatorioVencimientoService`
    (donde vivía duplicado inline) — ahora lo usan los dos, un solo lugar con la lógica de "clampear
    al último día del mes si no tiene ese día".
  - **Se muestra como línea aparte, nunca sumado al monto a cobrar** — mismo criterio que Gastos:
    en la grilla de Pagos y en las cuotas del contrato aparece como "+ $X punitorio (Nd)" debajo del
    monto esperado; al registrar un cobro se ve como aviso informativo, separado del total a
    registrar, así el operador decide si lo cobra aparte o no. No se fuerza a incluirlo.
  - DTOs: `PagoDto`/`PagoListDto` ganaron `montoPunitorio`, `diasAtraso`, `tasaPunitorioUsada` (todos
    calculados, no persistidos). `ContratoDto`/`Create`/`UpdateContratoRequest` ganaron
    `punitorioPorcentaje`. Formulario de contrato tiene un campo nuevo "Punitorio por mora — % diario
    fijo (opcional)" con la aclaración de que si se deja vacío usa la TIM del BCRA.
  - **Probado en vivo (2026-08-22)**: se backdateó a mano el período de una cuota de prueba para que
    quedara vencida (15/07/2026, 38 días de atraso a la fecha). El monto calculado en pantalla
    ($12.117,91) coincidió con la cuenta a mano usando los valores reales de TIM guardados en la base
    (163.034,3957 / 158.759,2172).

  **Registro de lo efectivamente cobrado — implementado 2026-08-22**: probando el flujo de cobro
  surgió la pregunta obvia — si el operador cobra la cuota + el punitorio, ¿queda registrado que una
  parte era interés, a qué tasa, cuántos días? Antes de este agregado, la respuesta era no: el
  operador tenía que sumar todo a mano en el campo Monto de "Formas de pago", y el sistema solo veía
  un "cobré de más" sin explicación, indistinguible de un error de tipeo.
  - Campos nuevos en `Pago`, todos nullable, **congelados al momento de cobrar** (a diferencia de
    `MontoPunitorio`/`DiasAtraso` que son en vivo y cambian todos los días): `MontoPunitorioCobrado`,
    `DiasAtrasoPunitorioCobrado`, `FechaVencimientoPunitorioCobrado`, y `DetallePunitorioCobrado`
    (texto libre con la fórmula/tasa completa — ej. `"TIM BCRA: 163034.3957 (22/08/2026) /
    158759.2172 (15/07/2026)"` o `"1.0000%/día fijo del contrato × 38 días de atraso..."` — así la
    tasa/índice queda legible sin necesitar columnas separadas para cada valor).
  - `UpdatePagoRequest` ganó `CobrarPunitorio` (bool). En el modal "Registrar cobro" el texto
    informativo se convirtió en un checkbox real: "Cobrar también el punitorio por N días: $X".
  - **El monto nunca lo manda el cliente** — cuando `CobrarPunitorio=true` y el nuevo estado es
    Pagado, `PagosController.UpdatePago` vuelve a llamar `IPunitorioService.CalcularAsync(pago)` del
    lado del servidor (con el `Estado` todavía Pendiente/Atrasado, antes de mutarlo) y congela ESE
    resultado — nunca confía en un número que venga del front. Es plata, no se toma de afuera.
  - Se ve en la grilla de Pagos (columna Cobrado) y en las cuotas del contrato, igual que el cálculo
    en vivo pero ya no cambia: queda fijo con lo que realmente se cobró ese día.
  - **También en el recibo** (probado, encontrado en el primer cobro real): tanto el cuerpo del email
    (`PagosController.BuildEmailBody`) como el PDF adjunto (`QuestPdfReportService.ComposeRecibo`)
    ahora desglosan Cuota / Punitorio (N días de atraso) antes del total, cuando
    `MontoPunitorioCobrado` tiene valor — antes solo mostraban un monto total sin explicar el
    excedente.
  - En el modal "Registrar cobro" el checkbox autocompleta el campo Monto con cuota + punitorio (solo
    si hay una única forma de pago sin editar a mano), y la pantalla de confirmación también desglosa
    Cuota/Punitorio antes de pedir confirmar — antes solo mostraba "Total cobrado (+$X vs esperado)",
    que se leía como un sobrepago sin explicar.

  **Interruptor por contrato — implementado 2026-08-22**: `Contrato.AplicaPunitorios` (`bool`, default
  **true**) — si está en `false`, no se calcula ni se muestra ningún punitorio para ese contrato,
  sin importar el % fijo ni la tasa TIM. Es un campo separado de `DiaVencimientoPago` (ese lo sigue
  usando el aviso de vencimiento próximo). En el formulario de contrato es un checkbox "Aplicar
  punitorios por mora a este contrato" — sirve para los casos donde la inmobiliaria o el propietario
  no quieren castigar el atraso de un inquilino puntual. Filosofía general: no todo tiene que ser
  obligatorio, el sistema debe ser flexible por contrato en vez de forzar una única regla para todos.

  **Se puede tocar sin importar el estado del contrato**: el `Update` general de `Contrato` solo
  permite editar en Borrador, pero `AplicaPunitorios`/`PunitorioPorcentaje` es configuración
  administrativa, no una condición económica congelada — necesita poder cambiarse en cualquier
  momento. Se agregó `PUT /api/contratos/{id}/punitorios` (mismo criterio que el endpoint de Ajuste de
  cuotas, que también funciona fuera del flujo de edición general), y un ícono de % en la grilla de
  Contratos (rojo si está activado, gris si no) que abre `PunitoriosModal.tsx` con el checkbox y el %.
- [x] **Gestión de Gastos** (prioridad alta) — implementado 2026-08-09, ver sección GASTOS.
- [ ] **Automatizar el ajuste periódico de cuotas** (prioridad alta, pendiente de análisis a fondo —
  2026-08-09, detectado probando el formulario de Contrato). Hoy `TipoAjuste` (`Fijo`/`IndiceICL`/
  `Porcentaje`/`Otro`) y `PeriodicidadAjusteMeses` son solo datos que se cargan en el contrato — no
  disparan nada solos. Todo ajuste de cuota hoy es 100% manual, vía `POST /api/contratos/{id}/ajustes`
  (ver sección CONTRATO/`AjusteModal.tsx`). Falta pensar el circuito completo automático, no solo el
  cálculo:
  - **Cálculo del nuevo monto** según `TipoAjuste`: `Porcentaje` es simple (ya existe la lógica manual
    de referencia en `AjusteModal`/`AplicarAjuste`), pero `IndiceICL` (y cualquier índice real tipo
    IPC) requiere traer un valor externo actualizado (¿API del BCRA/INDEC? ¿carga manual mensual de
    un valor de índice en el sistema?) — hoy no hay ninguna fuente de datos de índices conectada.
  - **Cuándo disparar**: necesita un proceso periódico (similar a `RecordatorioVencimientoService`)
    que revise, contrato por contrato, si ya se cumplió `PeriodicidadAjusteMeses` desde el último
    ajuste (`AjusteContrato` más reciente, o `FechaInicio` si nunca tuvo uno).
  - **Notificar el ajuste**: ya existe el tema `AvisoAumento` (ver NOTIFICACIONES) pero hoy solo se
    dispara cuando alguien aplica un ajuste a mano — habría que dispararlo también desde este proceso
    automático.
  - **Aplicar el nuevo monto**: decidir si se autoaplica directo (mueve `MontoActual`/genera
    `AjusteContrato` solo) o si queda como una "propuesta" que un Admin/Operador tiene que confirmar
    antes de que impacte en los próximos `Pago` — dado que es dinero, probablemente conviene un paso
    de confirmación humana, mismo criterio que se usó para no auto-completar datos de Liquidación con
    IA (ver ítem de arriba).
- [ ] WhatsApp como canal de notificación (hoy solo email). Para más adelante.
- [ ] Integración de facturación electrónica (ARCA/ex-AFIP). Para más adelante, alcance grande y
  específico de Argentina.
