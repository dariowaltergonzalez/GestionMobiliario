# GestionInmobiliaria — Guía de arquitectura y patrones

API REST en .NET 10 para gestión inmobiliaria. Seguir estos patrones en cada nuevo módulo.

---

## Reglas de trabajo

- **Siempre trabajar en una rama nueva** por cada módulo o feature: `git checkout -b feature/nombre-modulo`
- Nunca desarrollar directamente en `master`
- Hacer merge a `master` solo cuando el módulo esté completo y probado

---

## Estructura del proyecto

```
GestionInmobiliaria.Dominio/
  Entidades/          → Clases de dominio (Propietario, Inquilino, Propiedad, etc.)
  Interfaces/         → Contratos de repositorios (IPropietarioRepository, etc.)
  Common/             → Clases compartidas: PagedResult<T>, PaginationParams

GestionInmobiliaria.Aplicacion/
  DTOs/               → DTOs de entrada/salida, requests, combos

GestionInmobiliaria.Infraestructura/
  Persistencia/       → ApplicationDbContext (EF Core + Identity)
  Repositorios/       → Implementaciones de repositorios
  Extensions/         → PaginationExtensions (ToPagedResultAsync)
  Migrations/         → Migraciones de EF Core

GestionInmobiliaria.WebApi/
  Controllers/        → Endpoints REST
  HttpRequests/       → Archivos .http para testing manual
  Program.cs          → Configuración DI, JWT, Swagger
```

---

## Stack tecnológico

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 + SQL Server
- ASP.NET Identity + JWT (access token + refresh token)
- Swagger/OpenAPI

---

## Patrón de respuesta API

Todas las respuestas usan `ApiResponse<T>` definido en `GestionInmobiliaria.Aplicacion/DTOs/ApiResponse.cs`:

```json
{ "success": true, "data": { ... }, "message": "..." }
{ "success": false, "errors": ["..."] }
```

Usar siempre:
- `ApiResponse<T>.Ok(data)` — 200 con datos
- `ApiResponse<T>.Ok(data, "mensaje")` — 200 con datos y mensaje
- `ApiResponse<T>.Fail("mensaje")` — para errores

---

## Patrón de paginación

### Clases base (ya creadas, no recrear)
- `PagedResult<T>` → `GestionInmobiliaria.Dominio/Common/PagedResult.cs`
- `PaginationParams` → `GestionInmobiliaria.Dominio/Common/PaginationParams.cs`
- `PaginationExtensions` → `GestionInmobiliaria.Infraestructura/Extensions/PaginationExtensions.cs`

### Respuesta paginada
```json
{
  "success": true,
  "data": {
    "items": [...],
    "pagina": 1,
    "tamano": 10,
    "totalRegistros": 45,
    "totalPaginas": 5,
    "tienePaginaAnterior": false,
    "tienePaginaSiguiente": true
  }
}
```

### Query params estándar para grillas
```
GET /api/entidad?pagina=1&tamano=10&buscar=texto&activo=true
```

---

## Endpoints que debe tener cada módulo

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | `/api/entidad` | Listado paginado con filtros (para grilla) | Authorize |
| GET | `/api/entidad/activos` | Lista simple sin paginar (para combos/dropdowns) | Authorize |
| GET | `/api/entidad/{id}` | Detalle por ID | Authorize |
| POST | `/api/entidad` | Crear | Authorize |
| PUT | `/api/entidad/{id}` | Actualizar | Authorize |
| DELETE | `/api/entidad/{id}` | Baja lógica (Activo = false) | Authorize |

> Para Propiedades el endpoint de combo se llama `/disponibles` (Estado == Disponible) en lugar de `/activos`.

---

## DTOs que debe tener cada módulo

En `GestionInmobiliaria.Aplicacion/DTOs/NombreEntidadDto.cs`:

```csharp
// Para la grilla (GET paginado)
public class NombreEntidadDto { ... }

// Para crear
public class CreateNombreEntidadRequest { ... }

// Para actualizar
public class UpdateNombreEntidadRequest { ... }

// Para combos/dropdowns (GET /activos)
public class NombreEntidadComboDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}
```

---

## Interfaz de repositorio — estructura estándar

```csharp
public interface INombreEntidadRepository
{
    Task<PagedResult<NombreEntidad>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null);
    Task<IEnumerable<NombreEntidad>> GetActivosAsync();
    Task<NombreEntidad?> GetByIdAsync(int id);
    Task<NombreEntidad> CreateAsync(NombreEntidad entidad);
    Task<NombreEntidad> UpdateAsync(NombreEntidad entidad);
    Task<bool> DeleteAsync(int id);
}
```

---

## Repositorio — implementación estándar

```csharp
public async Task<PagedResult<NombreEntidad>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null)
{
    var query = _context.NombreEntidades.AsQueryable();

    if (activo.HasValue)
        query = query.Where(x => x.Activo == activo.Value);

    if (!string.IsNullOrWhiteSpace(buscar))
        query = query.Where(x => x.Nombre.Contains(buscar) || ...);

    query = query.OrderBy(x => x.Nombre);

    return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
}

public async Task<IEnumerable<NombreEntidad>> GetActivosAsync() =>
    await _context.NombreEntidades
        .Where(x => x.Activo)
        .OrderBy(x => x.Nombre)
        .ToListAsync();
```

---

## Controller — estructura estándar

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] PaginationParams paginacion,
    [FromQuery] string? buscar,
    [FromQuery] bool? activo)
{
    var resultado = await _repo.GetPagedAsync(paginacion, buscar, activo);
    var paginado = new PagedResult<NombreEntidadDto>
    {
        Items = resultado.Items.Select(MapToDto).ToList(),
        Pagina = resultado.Pagina,
        Tamano = resultado.Tamano,
        TotalRegistros = resultado.TotalRegistros,
        TotalPaginas = resultado.TotalPaginas
    };
    return Ok(ApiResponse<PagedResult<NombreEntidadDto>>.Ok(paginado));
}

[HttpGet("activos")]
public async Task<IActionResult> GetActivos()
{
    var lista = await _repo.GetActivosAsync();
    var dtos = lista.Select(x => new NombreEntidadComboDto { Id = x.Id, NombreCompleto = x.Nombre });
    return Ok(ApiResponse<IEnumerable<NombreEntidadComboDto>>.Ok(dtos));
}
```

---

## Auditoría (AuditLog)

`IAuditable` es una interfaz marcadora vacía. Toda entidad que la implemente queda auditada automáticamente en la tabla `AuditLogs`.

El `ApplicationDbContext` intercepta los cambios en `SaveChangesAsync` y registra:
- **INSERT** — valores nuevos en JSON + EntityId (capturado post-save)
- **UPDATE** — propiedades modificadas, valores anteriores y nuevos en JSON
- **DELETE** — valores anteriores en JSON
- Siempre guarda: `EntityName`, `Action`, `UserId`, `UserName`, `Timestamp`

**Regla: todos los modelos nuevos deben implementar `IAuditable`.**

```csharp
public class MiEntidad : IAuditable
{
    // ...
}
```

Las fechas `FechaCreacion` y `FechaActualizacion` se setean manualmente en el repositorio (ya no son parte de IAuditable):

```csharp
public async Task<MiEntidad> CreateAsync(MiEntidad entidad)
{
    entidad.FechaCreacion = DateTime.UtcNow;
    entidad.FechaActualizacion = DateTime.UtcNow;
    _context.MiEntidades.Add(entidad);
    await _context.SaveChangesAsync();
    return entidad;
}
```

---

## Checklist para agregar un nuevo módulo

- [ ] Entidad en `Dominio/Entidades/` — implementar `IAuditable` si tiene FechaCreacion/FechaActualizacion
- [ ] Interfaz en `Dominio/Interfaces/` — seguir estructura estándar
- [ ] DTOs en `Aplicacion/DTOs/` — Dto, CreateRequest, UpdateRequest, ComboDto
- [ ] Repositorio en `Infraestructura/Repositorios/` — implementar interfaz
- [ ] DbSet en `ApplicationDbContext` + configuración en `OnModelCreating`
- [ ] Registrar repositorio en `Program.cs`: `builder.Services.AddScoped<IRepo, Repo>()`
- [ ] Controller en `WebApi/Controllers/` — seguir estructura estándar
- [ ] Migración: `dotnet ef migrations add NombreMigracion --project GestionInmobiliaria.Infraestructura --startup-project GestionInmobiliaria.WebApi`
- [ ] Aplicar: `dotnet ef database update --project GestionInmobiliaria.Infraestructura --startup-project GestionInmobiliaria.WebApi`
- [ ] Archivo `.http` en `WebApi/HttpRequests/` con ejemplos de todos los endpoints

---

## Convenciones

- Baja lógica siempre: `Activo = false`, nunca DELETE físico
- Fechas en UTC: `DateTime.UtcNow`
- Nombres en español (entidades, propiedades, endpoints, variables)
- Ordenamiento por defecto: `Apellido` luego `Nombre` para personas; `Direccion` para propiedades
- El campo `buscar` filtra sobre texto libre (nombre, apellido, DNI, email según entidad)
- Parámetros de paginación por defecto: `pagina=1`, `tamano=10`, máximo `tamano=50`

---

## Módulos implementados

- **Auth** — register, login, refresh, logout, profile, activar/desactivar usuario
- **Propietarios** — CRUD + paginación + combo activos
- **Inquilinos** — CRUD + paginación + combo activos
- **Propiedades** — CRUD + paginación + combo disponibles
- **ConfiguracionEmpresa** — GET + PUT (singleton, solo Admin puede editar)

## Próximos módulos planificados

- **ContratoAlquiler** — vincula Propiedad + Inquilino, fechas, monto, duración
- **Pago** — pagos mensuales ligados a un contrato
