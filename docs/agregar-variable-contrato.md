# Cómo agregar una variable nueva al contrato

Cuando el admin necesita un campo que no aparece en el picker de variables, hay que tocar dos archivos (a veces tres si el campo no existe todavía en el DTO).

---

## Caso 1 — El campo ya existe en `ContratoDto`

> Ejemplo: agregar `{garante.email}` y el DTO ya tiene `GaranteEmail`.

### Paso 1 — Agregar la variable al picker

Archivo: `GestionInmobiliaria.WebApi/Controllers/ClausulasContratoController.cs`  
Método: `GetPlaceholders()`

Buscá el grupo correspondiente (en este ejemplo, `garante`) y agregá la entrada:

```csharp
new { clave = "{garante.email}", descripcion = "Email del garante" },
```

### Paso 2 — Agregar el reemplazo en el PDF

Archivo: `GestionInmobiliaria.Infraestructura/Services/QuestPdfReportService.cs`  
Método: `BuildPlaceholders()`

Buscá el bloque `// ── Garante` y agregá:

```csharp
{ "{garante.email}", c.GaranteEmail ?? "" },
```

Listo. El admin ya puede usar `{garante.email}` en cualquier cláusula y se reemplaza al generar el PDF.

---

## Caso 2 — El campo no existe en `ContratoDto`

> Ejemplo: agregar `{propiedad.barrio}` y `ContratoDto` no tiene ese campo.

### Paso 1 — Agregar el campo al DTO

Archivo: `GestionInmobiliaria.Aplicacion/DTOs/ContratoDto.cs`

```csharp
public string? PropiedadBarrio { get; set; }
```

### Paso 2 — Popularlo en la query del repositorio

Archivo: `GestionInmobiliaria.Infraestructura/Repositorios/ContratoRepository.cs`  
(o donde se construya el `ContratoDto`)

```csharp
PropiedadBarrio = contrato.Propiedad.Barrio,
```

### Paso 3 y 4 — Igual que el Caso 1

Agregar la variable en `GetPlaceholders()` y en `BuildPlaceholders()`.

---

## Caso 3 — El campo viene de una tabla nueva

> Ejemplo: agregar datos de un **seguro de caución** que antes no existía.

1. Crear la entidad y el repositorio siguiendo el checklist del `CLAUDE.md`
2. Agregar los campos necesarios a `ContratoDto`
3. Popularlo en la query que construye el DTO (puede requerir un JOIN nuevo en EF Core)
4. Seguir los pasos 1 y 2 del Caso 1

---

## Resumen rápido

| Situación | Archivos a tocar |
|-----------|-----------------|
| Campo ya en `ContratoDto` | `ClausulasContratoController.cs` + `QuestPdfReportService.cs` |
| Campo existe en la BD pero no en el DTO | + `ContratoDto.cs` + query del repositorio |
| Campo de tabla nueva | + entidad, migración, repositorio, JOIN |

---

## Grupos de variables disponibles

| Entidad | Prefijo |
|---------|---------|
| Locador | `{locador.*}` |
| Locatario | `{locatario.*}` |
| Propiedad | `{propiedad.*}` |
| Garante | `{garante.*}` |
| Contrato | `{contrato.*}` |
| Empresa | `{empresa.*}` |

Para agregar un grupo nuevo (entidad nueva), además de los pasos anteriores hay que agregar el bloque completo en `GetPlaceholders()` con su `entidad`, `etiqueta` y array de `campos`.
