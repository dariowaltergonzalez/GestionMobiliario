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

### Autocompletar comprobante con IA (2026-08-24)

El admin sube la foto/captura del comprobante de transferencia y el sistema precarga el formulario de
abono (monto, fecha, CBU/CVU, entidad, N° de operación) mandándole la imagen a un modelo de IA con
visión — **nunca autoguarda, siempre hay que revisar y confirmar antes de guardar** (es plata). La
imagen queda guardada en el sistema siempre, aunque la IA no encuentre nada.

- **Proveedor de IA aislado detrás de una interfaz** (`IReciboIaService.ExtraerDatosAsync`, en
  `Aplicacion/Services`), a pedido explícito del usuario: el día que convenga cambiar de proveedor
  (ej. a OpenAI cuando haya un cliente pagando), alcanza con crear una nueva implementación y cambiar
  el registro en `Program.cs` — ni el controller ni el frontend saben qué proveedor hay detrás.
- **Arrancó con Gemini** (`GeminiReciboIaService`, `Infraestructura/Services`) porque tiene tier
  gratis real sin tarjeta (a diferencia de OpenAI/Anthropic, que solo dan un crédito único de prueba)
  — sirve para desarrollar y probar sin costo. Usa `gemini-3.6-flash` con `responseSchema` (salida
  JSON forzada a un shape fijo: `monto`, `fecha`, `cbuCvuDestino`, `entidadDestino`,
  `numeroOperacion`, todos nullable — la IA nunca inventa un campo que no encuentra). Si falta
  `Gemini:ApiKey` en configuración o la llamada falla por cualquier motivo, el servicio no tira
  excepción — devuelve todos los campos en null y el usuario completa a mano; la imagen igual se
  guarda porque el guardado y la extracción son pasos independientes en el controller.
- **Ojo con el nombre del modelo, cambia seguido**: se arrancó con `gemini-2.5-flash` y la API
  devolvió 404 ("ya no está disponible para usuarios nuevos, usar gemini-3.6-flash") — se corrigió a
  `gemini-3.6-flash`. También puede devolver 503 "high demand" de forma transitoria (tier gratis) —
  no es un bug, con un reintento simple funciona. Si en el futuro vuelve a fallar con 404, lo primero
  a chequear es si el nombre del modelo venció de nuevo.
- **La API key es config global, no por tenant** (a diferencia de las credenciales SMTP, que sí son
  por tenant en `ConfiguracionEmpresa`) — es un costo/servicio de la plataforma, no algo que cada
  inmobiliaria configure. Vive en `Gemini:ApiKey`: placeholder vacío en `appsettings.json` (se
  commitea), la key real va en `appsettings.Development.json` (gitignoreado, `appsettings.*.json`
  salvo el propio `appsettings.json`).
- `POST /api/liquidaciones/comprobantes/extraer` — no depende de una `Liquidacion` puntual (se sube
  antes de saber a qué abono va a terminar asociada). Guarda el archivo con `IStorageService` (mismo
  patrón que fotos de `SolicitudTasacion`/`Propiedad`) en `uploads/{tenantId}/comprobantes/`, y
  devuelve `comprobanteUrl` + los datos extraídos en la misma respuesta.
- `LiquidacionAbono.ComprobanteUrl` (nullable) — un campo simple, no una tabla de documentos aparte
  (a diferencia de `DocumentoContrato`, que sí permite varios adjuntos): acá es 1 comprobante por
  abono, mismo criterio que `Contrato.ArchivoUrl`. Se ve como un ícono "Ver comprobante" al lado de
  cada abono ya cargado en `LiquidacionesPage.tsx`.
- **Probado en vivo (2026-08-24)** con una imagen de comprobante sintética (generada a propósito para
  la prueba, con datos ficticios: monto, fecha, CVU, entidad, N° de operación) y la API key real de
  Gemini: extrajo los 5 campos exactos, sin inventar ni errar ninguno. La imagen quedó guardada en
  `uploads/{tenantId}/comprobantes/` y se borró después de verificar, para no dejar datos de prueba.

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
  propietario se le muestren o no al inquilino) — ya se usa, ver sección PORTAL DE AUTOSERVICIO.

---

## PUNITORIOS

Recargo por mora en el pago de una cuota. Diseñado 2026-08-11, programado y probado de punta a punta
2026-08-22/24 (incluyendo un cobro real con punitorio, no solo la fórmula aislada).

**Investigación (2026-08-11)**: cómo se manejan los punitorios de alquiler en Argentina hoy —
- Marco legal: art. 768 CCyCN — la tasa aplicable es 1) lo pactado en el contrato, 2) ley especial,
  3) subsidiariamente la que fije el BCRA. Nuestra propia cláusula SÉPTIMA (plantilla de contrato) ya
  dice *"tasa activa por plazo fijo del Banco de la Nación Argentina"* como texto por defecto.
- Práctica de mercado: la mayoría pacta una tasa fija (~1% diario / 36% anual) o referencia la tasa
  BNA. Interés **simple** (lineal por día), no compuesto — así calculan los juzgados en estos casos.
- Competidor (Barreeo): su calculadora pide monto + fecha vencimiento + fecha pago + tasa del
  contrato, la marcan como "orientativa" ("depende de lo que diga el contrato"). En su plataforma
  completa se carga la regla de punitorios por contrato y el sistema calcula solo.
- Dato nuevo relevante: desde enero 2026 el BCRA publica una tasa pensada específicamente para esto,
  la **TIM (Tasa de Intereses Moratorios)**, vía Resolución 1/2026 — pensada como referencia oficial
  para que los tribunales calculen intereses moratorios (art. 768 CCyCN). Más prolija que perseguir la
  tasa BNA específica, que el banco solo publica en PDF (no hay API limpia para eso).

**Descubrimiento clave al programar (2026-08-22): la TIM NO es una tasa % periódica, es un ÍNDICE
ACUMULADO** (viene subiendo desde 1993-06-03, hoy vale ~163.000 — mismo mecanismo que el CER). Se usa
dividiendo dos valores del índice, nunca "días de atraso × tasa diaria":

```
recargo = MontoAdeudado × (Valor_TIM(fecha_de_cobro) / Valor_TIM(fecha_de_vencimiento) − 1)
```

Esto de hecho **resuelve solo** la duda de "interés simple o compuesto" — el BCRA ya lo resolvió del
lado de ellos al construir el índice, nosotros solo dividimos dos valores. Confirmado en vivo contra
`api.bcra.gob.ar/estadisticas/v4.0/monetarias/1197` (`idVariable=1197`, descripción *"Tasa de
Intereses Moratorios (TIM) CCC, art. 768(c)"*, periodicidad diaria, serie completa desde 1993-06-03).

**Decisiones tomadas con el usuario (2026-08-11)**:
- Campo nuevo en `Contrato` con un % fijo diario (mismo patrón Porcentaje/Monto que
  `ComisionLocador`) — este sí sería una tasa % simple tradicional, no un índice. **Regla híbrida**:
  si ese % fijo es > 0, se usa ese. Si está en 0/vacío, se usa la fórmula del índice TIM de arriba —
  así los contratos con la cláusula por defecto (que no cargan un % propio) también calculan algo.
- Cálculo del monto de punitorio: **en vivo**, al mostrar/cobrar la cuota — nunca se guarda un
  acumulado que se desactualiza.

**No es un proceso automático ni corre a una hora fija**: a diferencia de
`RecordatorioVencimientoService` o `TasaMoratoriaSchedulerService`, el punitorio **no tiene ningún
`BackgroundService` propio**. `IPunitorioService.CalcularAsync(pago)` se ejecuta **cada vez que se
pide la lista de Pagos o las cuotas de un contrato** (`GET /api/pagos`, `GET /api/contratos/{id}/pagos`)
— nunca al registrar un cobro específicamente, y nunca en un horario programado. El monto que se ve
es siempre "a hoy": si se vuelve a mirar la misma cuota al día siguiente, el número cambia solo (un
día más de atraso, y la tasa TIM puede haber cambiado). No se persiste en ningún lado. Lo único que sí
corre 1 vez por día es `TasaMoratoriaSchedulerService` — pero ese actualiza la *materia prima* (la
tabla `TasasMoratorias`), no calcula ningún punitorio; el cálculo en sí lee esa tabla al vuelo cuando
alguien pide ver una cuota.

### Infraestructura de la tasa TIM

- Entidad `TasaMoratoria` (`Dominio/Entidades/TasaMoratoria.cs`): `Fecha` (día que reporta el BCRA,
  índice único), `Valor` (`decimal(18,8)`, necesita esa precisión — el índice viene con hasta 4
  decimales sobre una base de 6 dígitos enteros), `Origen` = "BCRA", `FechaConsulta`. Es dato
  **global, no por tenant** (la tasa es la misma para todas las inmobiliarias) — a propósito no tiene
  campo `TenantId` ni `HasQueryFilter`; igual queda auditada en `AuditLogs` por implementar
  `IAuditable`.
- `ITasaMoratoriaService.ActualizarAsync()` (`Infraestructura/Services/TasaMoratoriaService.cs`): si
  la tabla está vacía hace una **carga histórica completa** (pagina de a 3000 registros — es el máximo
  que permite la API del BCRA por request; la serie completa son ~12.136 valores, entran en 5
  páginas); si ya hay datos, solo trae `desde = últimaFecha + 1 hasta = hoy`. Deserializa la respuesta
  de `api.bcra.gob.ar/estadisticas/v4.0/monetarias/1197` con DTOs internos (`JsonPropertyName`, la API
  devuelve todo en minúscula: `results`/`detalle`/`fecha`/`valor`). Funciona por HTTPS normal sin
  problemas de certificado (`AddHttpClient("Bcra", ...)` en `Program.cs`, sin handler especial).
- **Bug real encontrado probando y cómo se resolvió**: el `BackgroundService` arranca su primer ciclo
  apenas levanta la app (no espera las 24hs), así que al probar manualmente justo después de un
  arranque en frío, el disparo manual (`POST /actualizar`) y el ciclo automático corrieron al mismo
  tiempo — los dos vieron la tabla vacía, los dos dispararon la carga histórica completa, y chocaron
  al insertar las mismas fechas. Se solucionó con un `SemaphoreSlim` **estático** dentro de
  `TasaMoratoriaService` que serializa todas las llamadas a `ActualizarAsync()` dentro del proceso —
  el segundo llamado simplemente espera al primero y después no encuentra nada nuevo para traer. Mismo
  criterio de siempre: el disparo manual y el automático **son literalmente el mismo método**, nunca
  lógica duplicada.
- `TasaMoratoriaSchedulerService` (`BackgroundService`, mismo patrón que
  `RecordatorioVencimientoService`) — llama a `ActualizarAsync()` una vez por día.
- `TasasMoratoriasController`: `GET /api/tasas-moratorias/ultima` (valor vigente), `GET /api/tasas-moratorias`
  (histórico, filtrable por `desde`/`hasta`), `POST /api/tasas-moratorias/actualizar`
  (`Authorize(Roles="Admin")`, dispara el mismo servicio que el background job).
- Probado en vivo contra la base real: carga histórica completa insertó los 12.136 valores correctos
  (1993-06-03 a 2026-08-24), y una segunda corrida detectó correctamente "ya está al día" sin duplicar
  nada.

### Cálculo del punitorio en sí

- `Contrato.PunitorioPorcentaje` (`decimal(7,4)`, nullable) — % diario fijo, opcional.
- `IPunitorioService.CalcularAsync(Pago pago)` (`Infraestructura/Services/PunitorioService.cs`,
  requiere `pago.Contrato` cargado): si `Estado` no es Pendiente/Atrasado devuelve 0; si el contrato
  no tiene `DiaVencimientoPago` cargado devuelve 0; si la cuota no está vencida devuelve 0; si
  `PunitorioPorcentaje > 0` usa ese % simple diario (`Monto × %/100 × díasAtraso`); si no, usa la
  fórmula del índice TIM de arriba, buscando en `TasasMoratorias` el valor **más reciente disponible
  en o antes de** cada fecha. Si no hay ninguna tasa TIM cargada todavía, devuelve 0 en vez de
  inventar un número.
- Se extrajo `VencimientoCalculator.Calcular(periodo, diaVencimientoPago)`
  (`Dominio/Common/VencimientoCalculator.cs`) de adentro de `RecordatorioVencimientoService` (donde
  vivía duplicado inline) — ahora lo usan los dos.
- **Se muestra como línea aparte, nunca sumado al monto a cobrar** — mismo criterio que Gastos: en la
  grilla de Pagos y en las cuotas del contrato aparece como "+ $X punitorio (Nd)" debajo del monto
  esperado; al registrar un cobro se ve como aviso informativo, separado del total a registrar, así el
  operador decide si lo cobra aparte o no.
- DTOs: `PagoDto`/`PagoListDto` ganaron `montoPunitorio`, `diasAtraso`, `tasaPunitorioUsada` (todos
  calculados, no persistidos). `ContratoDto`/`Create`/`UpdateContratoRequest` ganaron
  `punitorioPorcentaje`. Formulario de contrato tiene un campo "Punitorio por mora — % diario fijo
  (opcional)" con la aclaración de que si se deja vacío usa la TIM del BCRA.
- **Probado en vivo**: se backdateó a mano el período de una cuota de prueba para que quedara vencida
  (15/07/2026, 38/39 días de atraso según el día de prueba). El monto calculado en pantalla coincidió
  con la cuenta a mano usando los valores reales de TIM guardados en la base.

### Registro de lo efectivamente cobrado

Probando el flujo de cobro surgió la pregunta obvia — si el operador cobra la cuota + el punitorio,
¿queda registrado que una parte era interés, a qué tasa, cuántos días? Antes de este agregado, la
respuesta era no: el operador tenía que sumar todo a mano en el campo Monto de "Formas de pago", y el
sistema solo veía un "cobré de más" sin explicación, indistinguible de un error de tipeo.

- Campos nuevos en `Pago`, todos nullable, **congelados al momento de cobrar** (a diferencia de
  `MontoPunitorio`/`DiasAtraso` que son en vivo y cambian todos los días): `MontoPunitorioCobrado`,
  `DiasAtrasoPunitorioCobrado`, `FechaVencimientoPunitorioCobrado`, y `DetallePunitorioCobrado` (texto
  libre con la fórmula/tasa completa — ej. `"TIM BCRA: 163157.0316 (23/08/2026) / 158759.2172
  (15/07/2026)"` o `"1.0000%/día fijo del contrato × 38 días de atraso..."` — así la tasa/índice queda
  legible sin necesitar columnas separadas para cada valor).
- `UpdatePagoRequest` ganó `CobrarPunitorio` (bool). En el modal "Registrar cobro" hay un checkbox:
  "Cobrar también el punitorio por N días: $X".
- **El monto nunca lo manda el cliente** — cuando `CobrarPunitorio=true` y el nuevo estado es Pagado,
  `PagosController.UpdatePago` vuelve a llamar `IPunitorioService.CalcularAsync(pago)` del lado del
  servidor (con el `Estado` todavía Pendiente/Atrasado, antes de mutarlo) y congela ESE resultado —
  nunca confía en un número que venga del front. Es plata, no se toma de afuera.
- Se ve en la grilla de Pagos (columna Cobrado) y en las cuotas del contrato, igual que el cálculo en
  vivo pero ya no cambia: queda fijo con lo que realmente se cobró ese día.
- **También en el recibo**: tanto el cuerpo del email (`PagosController.BuildEmailBody`) como el PDF
  adjunto (`QuestPdfReportService.ComposeRecibo`) desglosan Cuota / Punitorio (N días de atraso) antes
  del total, cuando `MontoPunitorioCobrado` tiene valor.
- En el modal "Registrar cobro" el checkbox autocompleta el campo Monto con cuota + punitorio (solo si
  hay una única forma de pago sin editar a mano), y la pantalla de confirmación también desglosa
  Cuota/Punitorio antes de pedir confirmar.
- **Probado en vivo de punta a punta (2026-08-23)**: cuota real backdateada a 39 días de atraso,
  cobrada con el checkbox tildado ($462.465,52 = $450.000 cuota + $12.465,52 punitorio), verificado en
  la base (`MontoPunitorioCobrado`, `DiasAtrasoPunitorioCobrado`, `DetallePunitorioCobrado` con los
  valores TIM exactos), en la grilla de Pagos, y en el recibo PDF/email.

### Interruptor por contrato

`Contrato.AplicaPunitorios` (`bool`, default **true**) — si está en `false`, no se calcula ni se
muestra ningún punitorio para ese contrato, sin importar el % fijo ni la tasa TIM. Es un campo
separado de `DiaVencimientoPago` (ese lo sigue usando el aviso de vencimiento próximo). En el
formulario de contrato es un checkbox "Aplicar punitorios por mora a este contrato" — sirve para los
casos donde la inmobiliaria o el propietario no quieren castigar el atraso de un inquilino puntual.
Filosofía general: no todo tiene que ser obligatorio, el sistema debe ser flexible por contrato en vez
de forzar una única regla para todos.

**Se puede tocar sin importar el estado del contrato**: el `Update` general de `Contrato` solo permite
editar en Borrador, pero `AplicaPunitorios`/`PunitorioPorcentaje` es configuración administrativa, no
una condición económica congelada — necesita poder cambiarse en cualquier momento. Se agregó
`PUT /api/contratos/{id}/punitorios` (mismo criterio que el endpoint de Ajuste de cuotas, que también
funciona fuera del flujo de edición general), y un ícono de % en la grilla de Contratos (rojo si está
activado, gris si no) que abre `PunitoriosModal.tsx` con el checkbox y el %.

### Relacionado: liquidación del punitorio al propietario

El punitorio cobrado se liquida al propietario neto de la comisión de la inmobiliaria (o sea, la
comisión también se calcula sobre el punitorio) porque `GenerarLiquidacionSiCorrespondeAsync` usa
`pago.MontoPagado` (cuota+punitorio) como `MontoCobrado`. No fue una decisión explícita, salió solo de
sumar el punitorio a `MontoPagado` — pero se investigó cómo se maneja en la práctica en Argentina y
**coincide con el circuito estándar** (se cobra la cuota + punitorio, se descuenta la comisión de
administración al total, y el neto se liquida al propietario). Confirmado, no queda nada pendiente.

---

## AJUSTE AUTOMÁTICO

Aplica solo el ajuste periódico de cuotas (según ICL/UVA/IPC o % fijo) sin pedir confirmación humana,
para los contratos que lo tienen habilitado. Diseñado e implementado de punta a punta 2026-08-24 —
los 3 índices (ICL, UVA, IPC) quedaron completos y probados en vivo el mismo día en dos sesiones
(ICL + Porcentaje primero, IPC + UVA después).

**Decisión de fondo**: la competencia (Barreeo, Ubiquo, mialquiler.ar) aplica el ajuste automático,
sin confirmación humana — se venden con "te olvidás de calcular aumentos". Se decidió ir por el mismo
camino en vez del criterio habitual de "mostrar y confirmar" (usado para OCR de comprobantes) — no
hacerlo así nos deja atrás de la competencia. A cambio: **opt-in por contrato**
(`Contrato.AjusteAutomatico`, default `false`, a diferencia de `AplicaPunitorios` que es default
`true` — acá cada contrato negoció una cláusula de ajuste específica, activarlo para todos de golpe
aplicaría aumentos no acordados) + **registro completo y auditable de cada ajuste** + **notificación
automática** (mismo tema `AvisoAumento` que ya usaba el ajuste manual).

### Infraestructura de los índices (ICL, UVA, IPC)

Mismo patrón que la TIM de Punitorios, pero cada índice en su propia tabla (decisión explícita del
usuario: no generalizar en un servicio único de "índices BCRA", mantener cada uno desacoplado aunque
implique código repetido).

- **ICL** (BCRA, `idVariable=40`, "Índice para Contratos de Locación") y **UVA** (BCRA,
  `idVariable=31`, "Unidad de Valor Adquisitivo") — entidades `IndiceIcl`/`IndiceUva`
  (`Dominio/Entidades/`), copias casi textuales de `TasaMoratoria`: `Fecha` (índice único), `Valor`
  (`decimal(18,8)`), `Origen`, `FechaConsulta`. `IIndiceIclService`/`IIndiceUvaService` son el mismo
  mecanismo que `TasaMoratoriaService` letra por letra (solo cambia el `idVariable`): carga histórica
  completa si la tabla está vacía (paginada de a 3000, el máximo del BCRA), incremental si ya hay
  datos, mismo `SemaphoreSlim` estático contra la carrera manual/automático. Schedulers y controllers
  (`GET /api/indices-icl(-uva)/ultima`, `GET /api/indices-icl(-uva)`,
  `POST /api/indices-icl(-uva)/actualizar` solo Admin) calcados de sus equivalentes de TIM.
- **IPC** (INDEC, serie `148.3_INIVELNAL_DICI_M_26`, "IPC Nacional Nivel General") — la integración
  genuinamente nueva: **API distinta a la del BCRA**
  (`apis.datos.gob.ar/series/api/series/?ids=...&format=json`), formato de respuesta distinto
  (`{"data": [["2026-07-01", 12076.3937], ...]}`, array de `[fecha, valor]` en vez de objetos, se
  parsea con `JsonElement`/`EnumerateArray()` porque no es un shape fijo) y **cadencia mensual, no
  diaria** — el INDEC publica un valor por mes. `IndiceIpcService` no pagina por `offset` como el
  BCRA (la serie completa desde dic-2016 entra en un solo pedido con `limit=1000`); para la carga
  incremental usa `start_date`. Entidad `IndiceIpc` con la misma forma que las otras dos. Se agregó
  `HttpClient("Indec")` en `Program.cs` apuntando a `apis.datos.gob.ar/`. Igual que ICL/UVA, tiene su
  scheduler diario y su controller (`/api/indices-ipc/...`) aunque los datos casi nunca cambien de un
  día para el otro — así no depende de saber la fecha exacta de publicación del INDEC.
- Los 3 son datos **globales, no por tenant** (sin `TenantId` ni `HasQueryFilter`), igual quedan
  auditados por `IAuditable`.
- `TipoAjuste` ganó dos valores nuevos al final del enum para no correr los existentes:
  `IndiceIPC = 5`, `IndiceUVA = 6` (`Fijo=1`, `IndiceICL=2`, `Porcentaje=3`, `Otro=4` quedan igual).
- **Probado en vivo (2026-08-24)**: ICL trajo 2269 valores nuevos (2020-07-01 a 2026-09-16), UVA trajo
  3821 (2016-03-31 a 2026-09-15), IPC trajo 116 (2016-12-01 a 2026-07-01) — los 3 sin errores en el
  primer arranque.

### El job de ajuste automático

- `AjusteAutomaticoService` (`BackgroundService`, revisa una vez por día, mismo patrón que
  `RecordatorioVencimientoService`/`TasaMoratoriaSchedulerService`). Por cada tenant activo, busca
  contratos `Vigente` + `AjusteAutomatico=true` + `PeriodicidadAjusteMeses` cargado, y dispara el
  ajuste si `hoy >= (FechaUltimoAjuste ?? FechaInicio) + PeriodicidadAjusteMeses`.
- Cálculo del nuevo monto según `TipoAjuste`: `Porcentaje` usa `Contrato.PorcentajeAjuste` simple;
  `IndiceICL`/`IndiceUVA`/`IndiceIPC` dividen el valor del índice a hoy sobre el valor en la fecha
  base (mismo mecanismo de índice acumulado que la TIM) — `nuevoMonto = montoActual × (Índice(hoy) /
  Índice(fechaBase))`, factorizado una sola vez en `CalcularPorIndiceAsync` (recibe qué tabla mirar
  como parámetro — esto es una función privada interna del servicio, no rompe la decisión de no
  generalizar `IndiceIcl`/`IndiceUva`/`IndiceIpc` como tablas/servicios de fetch, que siguen
  totalmente separados). `Fijo`/`Otro` no tienen cálculo automático definido, se saltean sin tocar
  nada.
- **El detalle de auditoría usa la fecha REAL del valor encontrado, no la fecha consultada** — bug
  chico encontrado probando IPC en vivo: como el IPC es mensual, buscar "el valor más reciente en o
  antes de hoy" puede devolver un valor de semanas atrás, y al principio el texto armaba
  `"IPC INDEC: 12076,3937 (24/08/2026) / ..."` mostrando la fecha de HOY al lado de un valor que en
  realidad correspondía al 01/07/2026 — auditoría engañosa aunque el cálculo en sí fuera correcto. Se
  corrigió para que `ValorIclEnFechaAsync`/`ValorUvaEnFechaAsync`/`ValorIpcEnFechaAsync` devuelvan
  también la fecha real del registro usado, y el detalle la muestre en vez de `hoy`/`fechaBase`. Para
  ICL/UVA (diarios) casi nunca cambia nada visualmente; para IPC (mensual) es la diferencia entre un
  detalle correcto y uno confuso.
- Al aplicar: actualiza `Contrato.MontoActual`, **solo** las cuotas `Pendiente`/`Atrasado` (nunca
  cuotas ya generadas con otra lógica ni cuotas `Pagado` — decisión explícita del usuario, mismo
  criterio que `AplicarAjuste` manual), crea un `AjusteContrato` con `Automatico=true` y
  `DetalleIndiceUsado` (texto libre con los valores exactos del índice en ambas fechas, mismo criterio
  que `DetallePunitorioCobrado` de Punitorios — ej. `"ICL BCRA: 35,4600 (23/08/2026) / 29,3900
  (01/01/2026)"`), y notifica por el tema `AvisoAumento` (mismo que el ajuste manual, sin tema nuevo).
  Una falla al notificar queda contenida en su propio `try/catch` y no aborta el ajuste ya aplicado.
- **Logging de cada ciclo, no solo cuando hay ajustes** (agregado a pedido del usuario, mismo criterio
  que `TasaMoratoriaSchedulerService`/`IndiceIclSchedulerService`): cada corrida deja una línea
  `AjusteAutomaticoService: ciclo OK. N contratos revisados, M ajustes aplicados.` — permite confirmar
  desde el log que el job corrió sin errores aunque no haya tocado ningún contrato ese día.

### Interruptor por contrato

`PUT /api/contratos/{id}/ajuste-automatico` (mismo criterio que `AplicaPunitorios`: configuración
administrativa, no una condición económica congelada, se puede tocar en cualquier estado del
contrato, no solo Borrador) — actualiza `AjusteAutomatico`, `TipoAjuste`, `PeriodicidadAjusteMeses` y
`PorcentajeAjuste` juntos. Ícono de refresh en la grilla de Contratos (azul si está activado, gris si
no) que abre `AjusteAutomaticoModal.tsx`. El formulario de Crear/Editar contrato también tiene el
checkbox. `DetalleContratoModal` muestra el estado (Activado/Desactivado).

### Probado en vivo de punta a punta (2026-08-24)

Se backdateó `FechaUltimoAjuste` de un contrato de prueba (con `AjusteAutomatico=true`,
`TipoAjuste=IndiceICL`, periodicidad 6 meses) a una fecha vencida y se reinició el backend para que el
job corriera su primer ciclo:

- Log del ciclo: `1 contratos con ajuste automático revisados, 1 ajustes aplicados.`
- `Contrato.MontoActual`: $450.000 → $542.939,78 (coeficiente ICL 35,46/29,39 = +20,65%).
- `AjustesContrato` creado con `Automatico=true` y el `DetalleIndiceUsado` correcto.
- Cuotas `Pagado` (2) intactas, las 23 `Pendiente` actualizadas al nuevo monto.
- Notificación al propietario: **omitida** correctamente porque no tiene el tema `AvisoAumento`
  habilitado en sus preferencias (opt-in respetado).
- Notificación al inquilino: intentó el envío real y falló por falta de conectividad SMTP del entorno
  de prueba (no es un bug) — el error quedó contenido y **no rompió el ciclo**, que igual terminó OK.
- Después de verificar todo, se revirtió el contrato de prueba a su estado original (monto, cuotas,
  se borró el `AjusteContrato` de prueba) para no dejar datos falseados.
- **`TipoAjuste=Porcentaje` probado en vivo también (2026-08-24)**, mismo contrato de prueba, 10% fijo:
  $450.000 → $495.000 exacto, `AjustesContrato` con `Porcentaje=10` y
  `DetalleIndiceUsado="10,00% aplicado automáticamente"`, mismo comportamiento con cuotas
  Pendiente/Pagado que ICL. Revertido después de verificar.
- **`TipoAjuste=IndiceIPC` probado en vivo (2026-08-24)**, mismo contrato: $450.000 → $521.882,36
  (coeficiente IPC 12.076,3937/10.413,0309 = +15,97%), `DetalleIndiceUsado="IPC INDEC: 12.076,3937
  (01/07/2026) / 10.413,0309 (01/01/2026)"` — con la fecha real del valor (01/07), no la de hoy, ya
  con el fix de arriba aplicado. Mismo comportamiento de cuotas que los demás tipos.
- **Regresión de `IndiceICL` re-verificada después del refactor** del helper compartido
  (`CalcularPorIndiceAsync`) — se volvió a correr el mismo escenario de ICL y dio el resultado
  esperado, sin romper nada. `TipoAjuste=IndiceUVA` no se probó de punta a punta en el job (mismo
  mecanismo exacto que ICL, ya verificado 2 veces), pero sí se confirmó la carga del índice en sí.
- Después de cada prueba se revirtió el contrato al estado original — no queda ningún dato de prueba
  en la base.

Con esto, los 3 índices (ICL, UVA, IPC) y los dos tipos no-índice (Fijo → sin cálculo automático,
Porcentaje) quedan implementados y verificados. No queda nada pendiente en este tema.

---

## PORTAL DE AUTOSERVICIO

Vista pública, sin login, para que el Inquilino vea su estado de cuenta y el Propietario vea sus
liquidaciones — sin entrar al panel interno. Implementado 2026-08-24, primera pieza pública del
sistema que no pasa por JWT.

### Acceso — investigado antes de decidir

Se investigó cómo lo resuelve la competencia (Barreeo, mialquiler.ar): **ninguno usa
usuario/contraseña**. Cita textual de Barreeo: *"Compartí el link con el inquilino... No necesita
instalar nada ni registrarse."* Se decidió ir por el mismo camino — un link con un token largo y
aleatorio, sin sesión ni registro. Es información de solo lectura (nada de pagos ni acciones desde
ahí), así que el nivel de seguridad de "link con secreto imposible de adivinar" alcanza.

### Diseño del token

- `TokenPortal` (string, único) en `Inquilino` y en `Propietario`. `null` hasta que se genera la
  primera vez.
- **Formato: `"{TenantId}.{secreto}"`** (`Dominio/Common/TokenPortal.cs`). El `TenantId` no es
  secreto — es solo dato de ruteo, para poder resolver a qué tenant pertenece el link **sin tener
  que escanear la tabla de todos los tenants** buscando el token (mismo problema que ya se había
  resuelto de otra forma, con `IgnoreQueryFilters()` + filtro manual, en los background services).
  Acá directamente se parsea el tenant del propio token antes de tocar la base. El secreto en sí son
  32 bytes de `RandomNumberGenerator` (criptográficamente aleatorio, no `Random` común).
- **Se genera (o regenera) desde `POST /api/{propietarios|inquilinos}/{id}/token-portal`** — un
  Admin/Operador lo dispara con el botón "Copiar link del portal" en la grilla correspondiente, que
  además copia la URL completa al portapapeles. Regenerar **invalida el link viejo automáticamente**
  (se pisa el valor, y el índice es único) — útil si se sospecha que el link se filtró.

### El controller público (`PortalController`)

- **A propósito no tiene `[Authorize]`** — no hay JWT, el token de la URL ES la credencial. Corre
  siempre con `IgnoreQueryFilters()` + `TenantId` explícito (parseado del token) en cada query, mismo
  patrón que los `BackgroundService`.
- `GET /api/portal/inquilino/{token}`: contrato Vigente (si tiene), histórico completo de cuotas
  (reusando `IPunitorioService.CalcularAsync` para el punitorio en vivo de la próxima cuota, igual
  que en el panel interno), y los `Gasto` con `Responsable=Inquilino` **atados a ese contrato
  puntual** (`ContratoId == contrato.Id`, no solo por Propiedad — así no se le muestra al inquilino
  actual algo que haya quedado cargado sin ContratoId de una relación anterior con la misma
  propiedad) y `VisibleParaInquilino=true`.
- `GET /api/portal/propietario/{token}`: todas sus Liquidaciones (cualquier propiedad), con el mismo
  desglose Cobrado/Comisión/Gastos/Abonado que ya tiene la grilla interna. **Agregado 2026-08-24**: el
  detalle expandible de cada Liquidación (fila con flecha ▼, mismo patrón que el panel interno) ahora
  muestra también cada transferencia recibida (fecha, medio, entidad, N° de operación, y un link "Ver
  comprobante" si el admin subió la foto — ver sección LIQUIDACIÓN → "Autocompletar comprobante con
  IA") y cada gasto descontado (categoría + descripción). Antes solo se veían los totales agregados;
  esto le da transparencia real al propietario sobre exactamente qué se le transfirió y por qué se
  descontó lo que se descontó, sin tener que preguntarle a la inmobiliaria.
- Un link inválido, mal formado, o de un tenant/persona inactiva siempre devuelve 404 con el mismo
  mensaje genérico ("Link inválido.") — no se filtra si el problema es "no existe" vs "está inactivo"
  vs "tenant no existe", para no dar pistas.

### Frontend

- Páginas nuevas en `pages/public/` (mismo lugar que la web pública de Propiedades), sin el layout
  del dashboard — pensadas para abrirse desde el celular, sin sidebar ni nada del panel interno.
  Rutas `/portal/inquilino/:token` y `/portal/propietario/:token`.
- El logo de la empresa (`ConfiguracionEmpresa.LogoUrl`) es una URL absoluta completa cargada a mano
  en Configuración (no un archivo servido por nuestra API con ruta relativa, a diferencia de las
  fotos de Propiedad) — se usa directo, sin anteponerle nada. Se encontró y corrigió este error
  probando: al principio se le anteponía la URL de la API como si fuera una ruta relativa.
- Probado en vivo end-to-end (token seteado a mano en la base para el test): ambos portales devuelven
  exactamente los datos esperados, coincidiendo con todo lo verificado a mano esta sesión (Gasto de
  $150.000, punitorio de $12.465,52, etc.) — y los casos de token mal formado / tenant inexistente
  devuelven 404 prolijo en vez de explotar.
- **Detalle de abonos/gastos del Portal Propietario probado en vivo (2026-08-24)**: se insertó un
  abono de prueba con comprobante y se confirmó que `GET /api/portal/propietario/{token}` lo devuelve
  completo (monto, medio, entidad, N° operación, `comprobanteUrl`) junto con el gasto real ya cargado
  en esa liquidación — se borró el abono de prueba después de verificar.

### Todavía no resuelto (para más adelante, no bloquea el uso básico)

- No hay ningún flujo para **mandar el link automáticamente** (ej. por email al activar el contrato)
  — hoy es 100% manual, un Admin lo copia y lo comparte a mano por el canal que prefiera.
  `INotificacionService` ya existe y se podría reusar para esto en el futuro.
  - Portal del Inquilino: solo muestra su contrato **Vigente** — un inquilino con varios contratos
  históricos en distintas propiedades no ve los viejos (no parece un problema real hoy, se anota por
  si en algún momento hace falta).

---

## DESPLIEGUE

Sistema desplegado en la nube para que el usuario le pase el link a sus contactos y lo prueben,
ideal desde el celular. Hecho y probado en vivo de punta a punta 2026-08-24/26. Plan elegido: **todo
gratis para arrancar** — cuando haya un cliente pagando, se puede migrar a algo pago sin drama, cada
pieza vive detrás de una interfaz swappeable (mismo criterio que `IReciboIaService`).

### Mapa — dónde vive cada cosa

| Pieza | Dónde | URL / identificador | Plan |
|---|---|---|---|
| Frontend (React) | Vercel | `https://gestion-mobiliario.vercel.app` | Hobby (gratis) |
| Backend (.NET API) | Render.com, como contenedor Docker | `https://gestioninmobiliaria-api.onrender.com` | Free |
| Base de datos | Azure SQL Database | servidor `servermobiliario.database.windows.net`, base `gestioninmobiliaria` | Oferta gratuita (Auto-pausa) |
| Storage de archivos (fotos, comprobantes, documentos) | Cloudinary | cloud name `ftrqrxmb` | Free (25 créditos/mes) |

Cuentas: Render y Vercel son cuentas nuevas del usuario logueadas con GitHub, con acceso restringido
solo al repo `GestionMobiliario` (no a todos sus repos). Azure SQL vive en la suscripción que el
usuario ya tenía (`dwg_free_basic`), en un grupo de recursos nuevo `sql_free` para no mezclarla con
su otro proyecto (GestionarticulosV3) — mismo usuario/contraseña que su otro servidor Azure, por
comodidad, decisión consciente del usuario. Cloudinary es una cuenta nueva.

### Cómo se actualiza

Tanto Render como Vercel tienen **autodeploy conectado a `master`** — cualquier `git push` dispara un
redeploy solo, sin tocar nada manualmente (Vercel tarda ~1 min, Render ~2-4 min por ser build de
Docker). La base de datos y Cloudinary no se "despliegan", solo se les pega en vivo.

### Variables de entorno (ninguna vive en el repo)

- **Render** (backend): `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`
  (cadena de Azure SQL), `Jwt__Key` (generada nueva para este despliegue — la del repo es un
  placeholder público, nunca se usó en producción), `Gemini__ApiKey`, `Cloudinary__CloudName`/
  `ApiKey`/`ApiSecret`.
- **Vercel** (frontend): `VITE_API_URL=https://gestioninmobiliaria-api.onrender.com`.
- **Local (dev)**: `appsettings.Development.json` (gitignoreado) para `Gemini:ApiKey`; el resto usa
  los valores por defecto de `appsettings.json` (SQL Server local, storage en disco).

### Problemas reales encontrados y resueltos (en orden)

1. **Azure CLI no funciona desde la máquina del usuario** — falla con
   `CERTIFICATE_VERIFY_FAILED`, probablemente proxy/antivirus corporativo de su red. Toda la creación
   de recursos en Azure se hizo a mano desde el Portal web, guiando paso a paso. Si se retoma con
   automatización (CLI/Terraform/etc.), primero hay que resolver esto o hacerlo desde otra
   máquina/red.
2. **Firewall de Azure SQL** — Render no es un servicio de Azure, así que la excepción "permitir
   servicios de Azure" no lo cubre, y el tier gratis de Render no tiene IP fija. Se agregó una regla
   de firewall `0.0.0.0`-`255.255.255.255` (la base sigue protegida por usuario/contraseña — práctica
   común para conectar servicios cloud sin IP fija).
3. **El `#` de la contraseña de Azure SQL se cortaba al pegar las variables con "Add from .env" de
   Render** — el parser trata `#` como inicio de comentario y descartaba el resto de la connection
   string (`Login failed for user 'dario'`). Se resolvió editando esa variable puntual directo en el
   campo individual, no por el pegado masivo.
4. **Las migraciones de EF Core crean tablas, no copian datos** — la base de Azure quedó con el
   esquema completo pero vacía. Se migraron los datos reales (tenants, usuarios, propiedades,
   contratos) desde SQL Server local con SSMS: `Tasks → Generate Scripts` sobre la base local, con
   **"Types of data to script" = "Data only"**, sacando la línea `USE [GestionInmobiliaria]` del
   script generado, y ejecutándolo contra la base de Azure. Errores esperados e inofensivos al
   correrlo: `__EFMigrationsHistory` (ya coincide en las dos bases) y `TasasMoratorias` (Azure ya la
   había completado sola al arrancar, con datos más frescos del BCRA).
5. **`npx tsc --noEmit` no detectaba los mismos errores que el build real** (`npm run build`, que usa
   `tsc -b` con project references) — el primer deploy en Vercel falló con 6 errores de TypeScript
   reales que nunca se habían visto en toda la sesión: campos faltantes en `TokenResponse`/
   `PropiedadDto` (`agenteId`/`videoUrl`, el backend ya los devolvía hace rato), un import de tipo sin
   `type` (`verbatimModuleSyntax`), y variables/imports sin usar. **Para verificar el frontend de acá
   en más, correr `npm run build` (el comando real), no `tsc --noEmit` solo.**
6. **El `.gitignore` del frontend ignoraba `src/pages/dashboard/logs/` entero** — la regla `logs`
   (heredada del template de Vite, pensada para carpetas de logs de npm/build) no tenía barra inicial,
   así que coincidía con cualquier carpeta llamada "logs" en cualquier nivel del proyecto, incluyendo
   la pantalla real de Logs del panel. `LogsPage.tsx` nunca había llegado a GitHub desde que se creó
   (meses atrás) — compilaba bien en local porque el archivo sí existía en el disco, pero Vercel clona
   desde GitHub y no lo encontraba. Se acotó la regla a `/logs` (solo la carpeta de la raíz) y se
   agregó el archivo. Se verificó que fuera el único afectado en todo el proyecto (frontend y
   backend), comparando los archivos del disco contra lo que git trackea realmente.
7. **CORS solo permitía `localhost`** — bloqueaba directamente al frontend ya desplegado
   (`gestion-mobiliario.vercel.app`), el navegador lo mostraba como "No se pudo conectar con el
   servidor" (no es un error de red real, es CORS). Se cambió para permitir también cualquier
   `*.vercel.app` (cubre producción y previews de Vercel automáticamente, sin tener que actualizar
   nada a mano) y una lista opcional `AllowedOrigins` (env var) para dominios propios futuros.
8. **Fotos de propiedades, video de propiedades y documentos de contrato nunca se migraron a
   Cloudinary** — cuando se armó el storage permanente (ver sección LIQUIDACIÓN → "Autocompletar
   comprobante con IA" y `IStorageService`), solo se conectó en 2 lugares (comprobantes de
   Liquidación, fotos de SolicitudTasacion). `PropiedadesController` (fotos y video) y
   `DocumentosContratoController` tenían su propio código que escribía directo a
   `_env.ContentRootPath` — se detectó porque las fotos de una propiedad de prueba aparecían rotas en
   el sistema ya desplegado (los archivos solo existían en el disco local de desarrollo, nunca
   llegaron a ningún lado accesible desde Render). Se migraron los tres a `IStorageService`, mismo
   patrón que los otros dos:
   - `PropiedadesController`: `SubirFotos`/`DeleteFoto`/`SubirVideo`/`DeleteVideo` reemplazan
     `File.Create`/`File.Delete` por `_storage.GuardarArchivoAsync`/`EliminarArchivoAsync`. Se sacó
     la dependencia de `IWebHostEnvironment` del controller (ya no hace falta).
   - `DocumentosContratoController`: mismo cambio en `Upload`/`Delete`. La acción `Download` cambió
     de comportamiento — antes leía los bytes del disco propio y los devolvía (`File(bytes, ...)`,
     preservando el nombre original de descarga); ahora hace `Redirect(doc.RutaRelativa)` a la URL
     guardada (funciona igual sea relativa —local— o absoluta —Cloudinary—), a costa de perder el
     nombre original bonito al descargar desde Cloudinary (el archivo baja con el nombre generado,
     no el que subió el usuario) — no se resolvió porque requeriría una transformación específica de
     Cloudinary (`fl_attachment`) que rompería la abstracción de `IStorageService`; se anota acá por
     si en algún momento se prioriza.
   - **Probado en vivo contra la cuenta real de Cloudinary**: subida y borrado de una foto de
     propiedad (confirmado con un `404` directo a la URL después de borrar) y de un documento de
     contrato (incluida la descarga, que devolvió el `302 Found` con el `Location` de Cloudinary
     correcto).
   - **Nota para pruebas locales futuras en modo Production**: además de `--no-launch-profile` (ver
     nota de abajo sobre el puerto), en modo Production real la app **no carga
     `appsettings.Development.json`** — si se prueba localmente forzando este ambiente, hay que pasar
     también `ConnectionStrings__DefaultConnection` explícita (la de `appsettings.json` apunta a
     `Server=localhost` a secas, no a la instancia real `.\SQLEXPRESS`), si no el login falla con un
     500 sin cuerpo (la excepción original de conexión rota hace que hasta el logueo del error a
     `AppLogs` falle, porque también necesita la base).
9. **Video de propiedad de >10MB fallaba al subir aun después de migrar a `IStorageService`** —
   `CloudinaryStorageService.GuardarArchivoAsync` subía todo con `resource_type=raw`, y el plan de
   Cloudinary limita "raw" a 10MB (video real de ~29.5MB fallaba con
   `File size too large. Got 31017054. Maximum is 10485760.`, visto directo en `AppLogs`). Primer
   intento de arreglo (detectar `.mp4`/`.mov`/etc. y pasar `"video"` como argumento `type` a
   `UploadAsync(RawUploadParams, string type)`) **no alcanzó** — se agregó un log de diagnóstico
   (`archivo=..., resourceType=...` en el mensaje de la excepción) y se vio en `AppLogs` que el
   backend sí calculaba `resourceType=video` pero Cloudinary igual devolvía el límite de 10MB. Se
   verificó en el dashboard de Cloudinary (Settings → Account → Usage Limits) que la cuenta real
   permite hasta **100MB para video** — descartando que fuera un límite de plan/cuenta. La causa real,
   encontrada decompilando el SDK `CloudinaryDotNet` 1.29.3 con `ilspycmd`: el overload
   `UploadAsync(RawUploadParams parameters, string type)` **ignora el argumento `type`** al armar la
   URL de subida — usa `GetUploadUrl(parameters)`, que lee `parameters.ResourceType`, una propiedad
   **de solo lectura hardcodeada a `ResourceType.Raw`** en `BasicRawUploadParams` (clase base de
   `RawUploadParams`). O sea: no importa qué string se le pase como `type`, siempre pegaba a
   `/raw/upload`. Arreglado usando el overload correcto,
   `UploadAsync(string resourceType, IDictionary<string, object> parameters, FileDescription
   fileDescription)`, que sí arma la URL con el `resourceType` real (`m_api.GetUploadUrl(resourceType)`).
   **Probado en vivo con un video real de 29.5MB — subida y persistencia confirmadas.** Lección para
   el futuro: si un método del SDK de Cloudinary "acepta" un parámetro pero el comportamiento no
   cambia, no asumir que el código propio está mal — puede ser el SDK ignorando el argumento;
   decompilar con `ilspycmd` (`dotnet tool install -g ilspycmd`) para confirmar antes de seguir
   probando variantes a ciegas.

### A tener en cuenta

- **No compartir el usuario admin real con los contactos que prueben** — hoy el acceso que se probó
  es `dariogonzalez08@gmail.com` (rol Admin) sobre el tenant `inmobiliaria-del-sur`. Si se les va a
  dar acceso a terceros, conviene crearles un usuario `Operador` aparte.
- **Render free se "duerme"** a los 15 min sin tráfico — el primer request después tarda ~30-60s en
  responder, y hasta puede fallar por un DNS transitorio en el primerísimo intento (reintentar
  alcanza). Ya no afecta a los archivos subidos (van a Cloudinary, no al disco de Render).
- Dato curioso confirmado en vivo: las respuestas del backend llegan servidas por Cloudflare desde un
  nodo en **Ezeiza (EZE)** — buena latencia para usuarios en Argentina pese a que la región del
  servicio en Render es Ohio (no hay región de Render en Sudamérica).
- **Nota técnica para pruebas locales futuras**: `dotnet run` ignora un `ASPNETCORE_ENVIRONMENT`
  seteado por variable de entorno porque `Properties/launchSettings.json` lo pisa con `"Development"`
  en el profile por defecto — para forzar otro ambiente en local hay que agregar
  `--no-launch-profile` (ojo: eso también hace que Kestrel ignore `applicationUrl` y caiga en el
  puerto 5000 por defecto en vez de 5005).
- Todavía no se probó desde un celular real (solo navegador de escritorio) — pendiente de que el
  usuario lo confirme.

---

## PENDIENTES GENERALES

Lista única de lo que falta, para no depender de la memoria de sesión a sesión. Se va tachando o
sacando a medida que se resuelve, como el resto del documento.

- [x] **Desplegar el sistema para que el usuario le pase el link a sus contactos y lo prueben, con
  acceso desde el celular** — hecho y probado en vivo de punta a punta 2026-08-24/26 (login, dashboard
  y datos reales funcionando desde el navegador contra el sistema desplegado). Ver sección
  DESPLIEGUE para el detalle completo de dónde vive cada cosa y los problemas reales que se
  encontraron en el camino.

- [x] **Anulación de cobros — DECIDIDO: no se implementa (analizado 2026-08-23/24)**. Surgió al ver
  que con el checkbox de punitorio es fácil cobrar mal por error y no había forma de corregirlo. Se
  analizó a fondo (ver historial de este ítem si hace falta retomar el detalle) y se llegó a la
  conclusión de que **no hace falta la funcionalidad**:
  - Los únicos motivos de anulación que se identificaron fueron 1) doble carga del mismo cobro y
    2) cargarlo contra el contrato equivocado. El primero no debería poder pasar nunca — no es algo
    que se "corrija", es algo que el sistema tiene que impedir de entrada. El segundo, en la práctica,
    es equivalente a un adelanto de cuota: cualquier diferencia de plata se resuelve con un ajuste en
    la próxima cuota, no reescribiendo el cobro ya asentado.
  - Encaja con el principio contable real investigado antes (Rentvine, AppFolio y demás): nunca se
    reescribe una transacción ya liquidada, toda corrección va hacia adelante. Si ese es el criterio
    general, "anular" un cobro específicamente no tiene un caso de uso real que lo justifique.
  - **Blindaje agregado igual, por las dudas** (2026-08-24): aunque el motivo #1 "no debería pasar
    nunca", no había ninguna validación real que lo impidiera del lado del servidor — lo único que
    evitaba re-cobrar una cuota ya Pagada era que el botón "Registrar cobro" desaparecía de la
    pantalla (una barrera de UI, no del backend). Se agregó el guard explícito en
    `PagosController.UpdatePago`: si `pago.Estado == Pagado`, rechaza con 400 antes de tocar nada.
    De paso se confirmó que `UpdateWithDetallesAsync` da de baja lógica las formas de pago viejas (no
    las borra físicamente), así que aunque alguien lo hubiera pisado, no se perdía el rastro.
  - El enum `EstadoPago.Anulado` (4) queda como está, sin usarse — no se saca porque no hace daño
    tenerlo, pero es dead code, igual que `EstadoPago.Atrasado` (ver ítem de abajo).
  - Hallazgo aparte encontrado en el camino, ya confirmado y sin nada pendiente — ver sección
    PUNITORIOS, "Relacionado: liquidación del punitorio al propietario".
- [ ] **Probar `AvisoVencimientoProximo` en la práctica** (implementado 2026-08-08, sin probar
  todavía). Difícil de replicar rápido porque depende de fechas de vencimiento reales — el chequeo
  corre una vez por día, así que verificar el circuito completo (7 días antes, 1 día antes, nada si
  ya venció) lleva varios días de prueba real, no algo que se pueda apurar con un botón. Para
  probarlo hace falta un contrato con `DiaVencimientoPago` cargado y una cuota Pendiente cuyo
  vencimiento caiga justo en alguna de esas ventanas.
- [x] **Autocompletar los datos de la transferencia de un abono de Liquidación a partir de una foto
  del comprobante** — implementado y probado en vivo de punta a punta 2026-08-24 con Gemini (IA con
  visión), detrás de una interfaz swappeable a otro proveedor. Ver sección LIQUIDACIÓN →
  "Autocompletar comprobante con IA". Sin nada pendiente.

### Ideas sacadas de investigar la competencia (Barreeo, 2026-08-09)

Barreeo es un competidor enfocado 100% en administración de alquileres (no cubre venta, tasaciones,
leads, agentes como nosotros). Se revisó su sitio para comparar funcionalidades. Prioridad acordada
con el usuario:

- [x] **Punitorios automáticos por mora** (implementado y probado en vivo 2026-08-22/24) — ver
  sección PUNITORIOS.
- [x] **Gestión de Gastos** (prioridad alta) — implementado 2026-08-09, ver sección GASTOS.
- [x] **Automatizar el ajuste periódico de cuotas** (ICL, UVA e IPC, prioridad alta) — implementado y
  probado en vivo de punta a punta 2026-08-24, los 3 índices. Ver sección AJUSTE AUTOMÁTICO. Sin nada
  pendiente.
- [ ] WhatsApp como canal de notificación (hoy solo email). Para más adelante.
- [ ] Integración de facturación electrónica (ARCA/ex-AFIP). Para más adelante, alcance grande y
  específico de Argentina.
