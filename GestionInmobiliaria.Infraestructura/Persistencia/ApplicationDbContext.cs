using GestionInmobiliaria.Dominio.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Persistencia;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Propietario> Propietarios => Set<Propietario>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
            e.Property(p => p.Direccion).HasMaxLength(300);
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
            e.Property(i => i.Direccion).HasMaxLength(300);
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
            e.Property(p => p.SuperficieTotal).HasColumnType("decimal(10,2)");
            e.Property(p => p.SuperficieCubierta).HasColumnType("decimal(10,2)");
            e.Property(p => p.Descripcion).HasMaxLength(1000);
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
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.FechaCreacion = DateTime.UtcNow;
                entry.Entity.FechaActualizacion = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.FechaActualizacion = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
