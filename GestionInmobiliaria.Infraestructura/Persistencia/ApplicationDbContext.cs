using System.Security.Claims;
using System.Text.Json;
using GestionInmobiliaria.Dominio.Entidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Persistencia;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Propietario> Propietarios => Set<Propietario>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa => Set<ConfiguracionEmpresa>();
    public DbSet<Agente> Agentes => Set<Agente>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            e.Property(a => a.Action).IsRequired().HasMaxLength(10);
            e.Property(a => a.EntityId).IsRequired().HasMaxLength(50);
            e.Property(a => a.UserId).HasMaxLength(450);
            e.Property(a => a.UserName).HasMaxLength(200);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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
                Timestamp = DateTime.UtcNow
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
