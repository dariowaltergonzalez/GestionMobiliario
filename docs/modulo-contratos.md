# Módulo Contratos — Definiciones y decisiones de diseño

## Contexto

Un contrato puede originarse desde una **Reserva** existente (flujo completo: Propiedad → Reserva → Contrato) o crearse directamente sin reserva previa. Cubre dos tipos de operación: **Locación** (alquiler) y **Boleto de Compraventa**.

---

## Entidades involucradas

### Contrato

| Campo | Tipo | Notas |
|---|---|---|
| `Codigo` | `string` | Auto-generado post-save: `CON-{año}-{id:D4}` |
| `Tipo` | enum | 1=Locacion, 2=BoletoCompraventa |
| `Estado` | enum | 1=Borrador, 2=Vigente, 3=Finalizado, 4=Rescindido |
| `PropiedadId` | FK real | ON DELETE NO ACTION |
| `ReservaId` | FK nullable | ON DELETE SET NULL — puede no venir de una reserva |
| `AgenteId` | FK nullable | ON DELETE SET NULL |
| `PropietarioRefId` | `int?` | **Sin FK real**, solo índice. Para joins manuales cuando el propietario existe en el sistema |
| `InquilinoRefId` | `int?` | **Sin FK real**, solo índice. Igual que arriba |
| `LocadorNombre/Apellido/Dni/Email/Telefono` | snapshot | Inmutable — refleja datos al momento de la firma |
| `LocatarioNombre/Apellido/Dni/Email/Telefono` | snapshot | Inmutable |
| `GaranteNombre/Apellido/Dni/Telefono` | snapshot nullable | Inmutable, garante opcional |
| `MontoBase` | `decimal(18,2)` | Monto inicial pactado |
| `Moneda` | enum | 1=ARS, 2=USD |
| `TipoAjuste` | enum | 1=Fijo, 2=IndiceICL, 3=Porcentaje, 4=Otro |
| `PeriodicidadAjusteMeses` | `int?` | Cada cuántos meses se ajusta |
| `DiaVencimientoPago` | `int?` | Solo Locacion (ej: 5 = vence el día 5 de cada mes) |
| `ComisionLocadorPorcentaje` | `decimal(5,2)?` | % que paga el locador/vendedor a la inmobiliaria |
| `ComisionLocadorMonto` | `decimal(18,2)?` | Monto fijo alternativo al porcentaje (locador/vendedor) |
| `ComisionLocatarioPorcentaje` | `decimal(5,2)?` | % que paga el locatario/comprador a la inmobiliaria |
| `ComisionLocatarioMonto` | `decimal(18,2)?` | Monto fijo alternativo al porcentaje (locatario/comprador) |
| `AdministracionCobros` | `bool` | Si `true`, el sistema gestiona los cobros (genera Pagos). Si `false`, la inmobiliaria solo armó el contrato y no interviene en el cobro mensual |
| `FechaInicio` | `DateTime` | |
| `FechaFin` | `DateTime?` | Null en BoletoCompraventa |
| `FechaEscrituracion` | `DateTime?` | Solo BoletoCompraventa |
| `Observaciones` | `string?` | |
| `ArchivoUrl` | `string?` | PDF del contrato firmado |

### Pago

| Campo | Tipo | Notas |
|---|---|---|
| `ContratoId` | FK | ON DELETE CASCADE |
| `NumeroCuota` | `int` | 1, 2, 3... (mes de alquiler o cuota de compraventa) |
| `Periodo` | `DateTime` | Mes/año que corresponde |
| `MontoEsperado` | `decimal(18,2)` | Monto que debería pagarse (puede variar por ajuste) |
| `MontoPagado` | `decimal(18,2)?` | Lo que realmente se pagó |
| `FechaPago` | `DateTime?` | Cuándo se registró el pago |
| `Estado` | enum | 1=Pendiente, 2=Pagado, 3=Atrasado, 4=Anulado |
| `Observaciones` | `string?` | |

Índice único: `(ContratoId, NumeroCuota)` — evita cuotas duplicadas.

---

## Comisiones en Reserva

La entidad `Reserva` también incorpora campos de comisión doble, porque en una compraventa la comisión ya se negocia al momento de la seña:

| Campo | Tipo | Notas |
|---|---|---|
| `ComisionVendedorPorcentaje` | `decimal(5,2)?` | % que paga el vendedor a la inmobiliaria |
| `ComisionVendedorMonto` | `decimal(18,2)?` | Monto fijo alternativo |
| `ComisionCompradorPorcentaje` | `decimal(5,2)?` | % que paga el comprador a la inmobiliaria |
| `ComisionCompradorMonto` | `decimal(18,2)?` | Monto fijo alternativo |

Al crear un Contrato desde una Reserva, estos campos se pre-populan automáticamente en los campos equivalentes del Contrato (Locador ← Vendedor, Locatario ← Comprador).

**¿Por qué comisión doble?**
En Argentina la inmobiliaria puede cobrarle a ambas partes. Una agencia puede cobrar 3% al vendedor y 3% al comprador en una venta, o un mes de alquiler al locatario y medio mes al locador en una locación. El software no impone ningún modelo — el usuario completa lo que aplica y deja en cero lo que no cobra.

---

## Reglas de negocio

### ¿Por qué snapshot + RefId opcional?

Los campos snapshot (Locador, Locatario, Garante) son **inmutables**: reflejan los datos exactos al momento de la firma, igual que en un contrato físico. Si el propietario cambia de teléfono o el inquilino actualiza su DNI, el contrato no cambia.

`PropietarioRefId` e `InquilinoRefId` son **int? sin FK real** (solo índices). Sirven para joins manuales cuando la persona existe como entidad en el sistema. Si no existe (cliente nuevo que nunca fue cargado), quedan en null.

### ¿Por qué `AdministracionCobros`?

En Argentina hay dos modelos de negocio:
- **Solo intermediación**: la inmobiliaria arma el contrato y cobra su comisión. El cobro mensual queda entre locador e inquilino — el sistema no interviene.
- **Administración de alquileres**: la inmobiliaria cobra una comisión extra (5-10% mensual) por hacerse cargo de la cobranza mensual y la liquidación al propietario.

Este flag desacopla ambos modelos sin necesidad de dos entidades distintas.

### Generación automática de Pagos

Los `Pago` se generan **automáticamente** cuando:
- `AdministracionCobros = true` **Y**
- El contrato se crea o actualiza con `Estado = Vigente`

Se genera **un `Pago` por mes** entre `FechaInicio` y `FechaFin`, todos con `Estado = Pendiente`. El usuario los va marcando como pagados a medida que se cobran.

Si el contrato queda en `Borrador`, no se generan pagos todavía.

### Sincronización de estado de Propiedad

El repositorio de Contratos actualiza automáticamente el estado de la propiedad vinculada:

**Locación:**
| Estado contrato | Estado propiedad |
|---|---|
| Borrador | Sin cambio |
| Vigente | `Alquilada` |
| Finalizado / Rescindido | `Disponible` |

**Boleto de Compraventa:**
| Estado contrato | Estado propiedad |
|---|---|
| Borrador | Sin cambio |
| Vigente | `BoletoFirmado` |
| Finalizado | `Vendida` |
| Rescindido | `Disponible` |

### Código de contrato

Se genera post-save, igual que el código de propiedad:
```
CON-{año}-{id:D4}
→ CON-2026-0001, CON-2026-0002, ...
```

---

## Flujo típico

```
Propiedad disponible
    ↓
Reserva (opcional) → Propiedad: Reservada
    ↓
Contrato Borrador → sin cambio en propiedad
    ↓
Contrato Vigente → Propiedad: Alquilada / BoletoFirmado
    + genera Pagos automáticos si AdministracionCobros = true
    ↓
Contrato Finalizado → Propiedad: Disponible / Vendida
```

---

## Pendiente / decisiones futuras

- [ ] Si en el futuro se implementa liquidación al propietario (inmobiliaria descuenta su comisión y transfiere el resto), se agrega un campo `MontoLiquidado` o una entidad `Liquidacion` vinculada al Pago.
- [ ] `ContratoDocumento` (adjuntos adicionales al PDF) — no es parte del MVP.
- [ ] `AjusteContrato` (historial explícito de ajustes de precio) — no es parte del MVP; los ajustes quedan implícitos en `Pago.MontoEsperado` que varía cuota a cuota.
