using System.Security.Claims;
using System.Text.Json;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Persistencia;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantService _tenantService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor,
        ITenantService tenantService) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantService = tenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Propietario> Propietarios => Set<Propietario>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa => Set<ConfiguracionEmpresa>();
    public DbSet<Agente> Agentes => Set<Agente>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<EventoAgenda> EventosAgenda => Set<EventoAgenda>();
    public DbSet<SolicitudTasacion> SolicitudesTasacion => Set<SolicitudTasacion>();
    public DbSet<FotoSolicitud> FotosSolicitud => Set<FotoSolicitud>();
    public DbSet<FotoPropiedad> FotosPropiedad => Set<FotoPropiedad>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppLog> AppLogs => Set<AppLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Filtros globales por tenant (se aplican automáticamente en cada query)
        builder.Entity<Propietario>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<Inquilino>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<Propiedad>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<Agente>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<Lead>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<EventoAgenda>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<ConfiguracionEmpresa>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<AppLog>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<AuditLog>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<ApplicationUser>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<SolicitudTasacion>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<FotoSolicitud>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<FotoPropiedad>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));

        builder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Nombre).IsRequired().HasMaxLength(200);
            e.Property(t => t.Slug).IsRequired().HasMaxLength(100);
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasData(new Tenant { Id = 1, Nombre = "Demo", Slug = "demo", Activo = true, FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        });

        builder.Entity<Propietario>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            e.Property(p => p.Apellido).IsRequired().HasMaxLength(100);
            e.Property(p => p.Dni).HasMaxLength(20);
            e.Property(p => p.Cuit).HasMaxLength(20);
            e.Property(p => p.Email).HasMaxLength(200);
            e.Property(p => p.Telefono).HasMaxLength(50);
            e.Property(p => p.Telefono2).HasMaxLength(50);
            e.Property(p => p.Direccion).HasMaxLength(300);
            e.Property(p => p.CBU).HasMaxLength(50);
            e.Property(p => p.Notas).HasMaxLength(1000);
        });

        builder.Entity<Inquilino>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Nombre).IsRequired().HasMaxLength(100);
            e.Property(i => i.Apellido).IsRequired().HasMaxLength(100);
            e.Property(i => i.Dni).HasMaxLength(20);
            e.Property(i => i.Cuit).HasMaxLength(20);
            e.Property(i => i.Email).HasMaxLength(200);
            e.Property(i => i.Telefono).HasMaxLength(50);
            e.Property(i => i.Telefono2).HasMaxLength(50);
            e.Property(i => i.Direccion).HasMaxLength(300);
            e.Property(i => i.Ocupacion).HasMaxLength(100);
            e.Property(i => i.NombreGarante).HasMaxLength(200);
            e.Property(i => i.TelefonoGarante).HasMaxLength(50);
            e.Property(i => i.DniGarante).HasMaxLength(20);
            e.Property(i => i.Notas).HasMaxLength(1000);
        });

        builder.Entity<Propiedad>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Direccion).IsRequired().HasMaxLength(300);
            e.Property(p => p.Barrio).HasMaxLength(100);
            e.Property(p => p.Ciudad).HasMaxLength(100);
            e.Property(p => p.Provincia).HasMaxLength(100);
            e.Property(p => p.Piso).HasMaxLength(10);
            e.Property(p => p.NumeroDepartamento).HasMaxLength(20);
            e.Property(p => p.PrecioAlquiler).HasColumnType("decimal(18,2)");
            e.Property(p => p.PrecioVenta).HasColumnType("decimal(18,2)");
            e.Property(p => p.Expensas).HasColumnType("decimal(18,2)");
            e.Property(p => p.SuperficieTotal).HasColumnType("decimal(10,2)");
            e.Property(p => p.SuperficieCubierta).HasColumnType("decimal(10,2)");
            e.Property(p => p.NroCatastro).HasMaxLength(100);
            e.Property(p => p.Descripcion).HasMaxLength(1000);
            e.Property(p => p.Notas).HasMaxLength(1000);
            e.HasOne(p => p.Propietario)
             .WithMany(o => o.Propiedades)
             .HasForeignKey(p => p.PropietarioId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Token).IsRequired().HasMaxLength(500);
            e.Property(r => r.UserId).IsRequired().HasMaxLength(450);
            e.Ignore(r => r.EstaRevocado);
            e.Ignore(r => r.EstaExpirado);
            e.Ignore(r => r.EsValido);
        });

        builder.Entity<Agente>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).IsRequired().HasMaxLength(450);
            e.Property(a => a.Zona).HasMaxLength(100);
            e.Property(a => a.TelefonoInterno).HasMaxLength(50);
            e.Property(a => a.ComisionPorcentaje).HasColumnType("decimal(5,2)");
            e.Property(a => a.Notas).HasMaxLength(1000);
            e.HasOne(a => a.User)
             .WithOne()
             .HasForeignKey<Agente>(a => a.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(a => a.Propiedades)
             .WithOne(p => p.Agente)
             .HasForeignKey(p => p.AgenteId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(a => a.Inquilinos)
             .WithOne(i => i.Agente)
             .HasForeignKey(i => i.AgenteId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ConfiguracionEmpresa>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.NombreComercial).IsRequired().HasMaxLength(200);
            e.Property(c => c.RazonSocial).HasMaxLength(200);
            e.Property(c => c.Cuit).HasMaxLength(20);
            e.Property(c => c.CondicionFiscal).HasMaxLength(100);
            e.Property(c => c.LogoUrl).HasMaxLength(500);
            e.Property(c => c.Slogan).HasMaxLength(300);
            e.Property(c => c.Telefono).HasMaxLength(50);
            e.Property(c => c.WhatsApp).HasMaxLength(50);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.SitioWeb).HasMaxLength(300);
            e.Property(c => c.Direccion).HasMaxLength(300);
            e.Property(c => c.Ciudad).HasMaxLength(100);
            e.Property(c => c.Provincia).HasMaxLength(100);
            e.Property(c => c.CodigoPostal).HasMaxLength(20);
            e.Property(c => c.Pais).HasMaxLength(100);
            e.Property(c => c.Instagram).HasMaxLength(300);
            e.Property(c => c.Facebook).HasMaxLength(300);
            e.Property(c => c.Twitter).HasMaxLength(300);
        });

        builder.Entity<Lead>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Nombre).IsRequired().HasMaxLength(100);
            e.Property(l => l.Apellido).IsRequired().HasMaxLength(100);
            e.Property(l => l.Email).HasMaxLength(200);
            e.Property(l => l.Telefono).HasMaxLength(50);
            e.Property(l => l.Notas).HasMaxLength(1000);
            e.HasOne(l => l.Agente)
             .WithMany(a => a.Leads)
             .HasForeignKey(l => l.AgenteId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.Propiedad)
             .WithMany()
             .HasForeignKey(l => l.PropiedadId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<EventoAgenda>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Notas).HasMaxLength(1000);
            e.HasOne(ev => ev.Agente)
             .WithMany(a => a.Eventos)
             .HasForeignKey(ev => ev.AgenteId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ev => ev.Propiedad)
             .WithMany()
             .HasForeignKey(ev => ev.PropiedadId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ev => ev.Lead)
             .WithMany(l => l.Eventos)
             .HasForeignKey(ev => ev.LeadId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ev => ev.Inquilino)
             .WithMany()
             .HasForeignKey(ev => ev.InquilinoId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SolicitudTasacion>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Nombre).IsRequired().HasMaxLength(100);
            e.Property(s => s.Apellido).IsRequired().HasMaxLength(100);
            e.Property(s => s.Email).HasMaxLength(200);
            e.Property(s => s.Telefono).IsRequired().HasMaxLength(50);
            e.Property(s => s.Direccion).IsRequired().HasMaxLength(300);
            e.Property(s => s.Barrio).HasMaxLength(100);
            e.Property(s => s.Ciudad).HasMaxLength(100);
            e.Property(s => s.SuperficieTotal).HasColumnType("decimal(10,2)");
            e.Property(s => s.SuperficieCubierta).HasColumnType("decimal(10,2)");
            e.Property(s => s.Descripcion).HasMaxLength(2000);
            e.Property(s => s.NotasInternas).HasMaxLength(2000);
            e.Property(s => s.ValorEstimado).HasColumnType("decimal(18,2)");
            e.HasOne(s => s.Agente)
             .WithMany()
             .HasForeignKey(s => s.AgenteId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(s => s.Fotos)
             .WithOne(f => f.SolicitudTasacion)
             .HasForeignKey(f => f.SolicitudTasacionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FotoSolicitud>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Url).IsRequired().HasMaxLength(500);
            e.Property(f => f.NombreArchivo).HasMaxLength(255);
        });

        builder.Entity<FotoPropiedad>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Url).IsRequired().HasMaxLength(500);
            e.Property(f => f.NombreArchivo).HasMaxLength(255);
            e.HasOne(f => f.Propiedad)
             .WithMany(p => p.Fotos)
             .HasForeignKey(f => f.PropiedadId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            e.Property(a => a.Action).IsRequired().HasMaxLength(10);
            e.Property(a => a.EntityId).IsRequired().HasMaxLength(50);
            e.Property(a => a.UserId).HasMaxLength(450);
            e.Property(a => a.UserName).HasMaxLength(200);
        });

        builder.Entity<AppLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Origen).IsRequired().HasMaxLength(200);
            e.Property(a => a.Mensaje).IsRequired().HasMaxLength(2000);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-setear TenantId en todas las entidades nuevas
        var tenantId = _tenantService.TenantId ?? 0;
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added))
        {
            var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
            if (prop != null && (int)prop.CurrentValue! == 0)
                prop.CurrentValue = tenantId;
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        var auditorias = new List<(AuditLog Log, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry)>();

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var audit = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                TenantId = _tenantService.TenantId ?? 0
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    audit.Action = "INSERT";
                    audit.NewValues = JsonSerializer.Serialize(
                        entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                    auditorias.Add((audit, entry));
                    break;

                case EntityState.Deleted:
                    audit.Action = "DELETE";
                    audit.EntityId = entry.Properties
                        .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";
                    audit.OldValues = JsonSerializer.Serialize(
                        entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                    AuditLogs.Add(audit);
                    break;

                case EntityState.Modified:
                    audit.Action = "UPDATE";
                    audit.EntityId = entry.Properties
                        .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";
                    var changed = entry.Properties.Where(p => p.IsModified).ToList();
                    audit.ChangedProperties = string.Join(", ", changed.Select(p => p.Metadata.Name));
                    audit.OldValues = JsonSerializer.Serialize(
                        changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                    audit.NewValues = JsonSerializer.Serialize(
                        changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                    AuditLogs.Add(audit);
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Para INSERT: el Id se genera en la DB, lo capturamos después del save
        foreach (var (log, entry) in auditorias)
        {
            log.EntityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";
            AuditLogs.Add(log);
        }

        if (auditorias.Any())
            await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}
