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
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PagoDetalle> PagoDetalles => Set<PagoDetalle>();
    public DbSet<FotoSolicitud> FotosSolicitud => Set<FotoSolicitud>();
    public DbSet<FotoPropiedad> FotosPropiedad => Set<FotoPropiedad>();
    public DbSet<ClausulaContrato> ClausulasContrato => Set<ClausulaContrato>();
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
        builder.Entity<Reserva>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0) && e.Activo);
        builder.Entity<Contrato>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0) && e.Activo);
        builder.Entity<Pago>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0) && e.Activo);
        builder.Entity<PagoDetalle>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0) && e.Activo);
        builder.Entity<FotoSolicitud>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<FotoPropiedad>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));
        builder.Entity<ClausulaContrato>().HasQueryFilter(e => e.TenantId == (_tenantService.TenantId ?? 0));

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
            e.Property(p => p.Banco).HasMaxLength(100);
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
            e.Property(p => p.Codigo).IsRequired().HasMaxLength(20).HasDefaultValue(string.Empty);
            e.HasIndex(p => p.Codigo).IsUnique();
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

        builder.Entity<Reserva>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.CompradorNombre).IsRequired().HasMaxLength(100);
            e.Property(r => r.CompradorApellido).IsRequired().HasMaxLength(100);
            e.Property(r => r.CompradorDni).HasMaxLength(20);
            e.Property(r => r.CompradorTelefono).HasMaxLength(50);
            e.Property(r => r.CompradorEmail).HasMaxLength(200);
            e.Property(r => r.VendedorNombre).IsRequired().HasMaxLength(100);
            e.Property(r => r.VendedorApellido).IsRequired().HasMaxLength(100);
            e.Property(r => r.VendedorDni).HasMaxLength(20);
            e.Property(r => r.VendedorTelefono).HasMaxLength(50);
            e.Property(r => r.VendedorEmail).HasMaxLength(200);
            e.Property(r => r.MontoSenia).HasColumnType("decimal(18,2)");
            e.Property(r => r.PrecioTotal).HasColumnType("decimal(18,2)");
            e.Property(r => r.ComisionVendedorPorcentaje).HasColumnType("decimal(5,2)");
            e.Property(r => r.ComisionVendedorMonto).HasColumnType("decimal(18,2)");
            e.Property(r => r.ComisionCompradorPorcentaje).HasColumnType("decimal(5,2)");
            e.Property(r => r.ComisionCompradorMonto).HasColumnType("decimal(18,2)");
            e.Property(r => r.Observaciones).HasMaxLength(2000);
            e.HasOne(r => r.Propiedad)
             .WithMany()
             .HasForeignKey(r => r.PropiedadId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Lead)
             .WithMany()
             .HasForeignKey(r => r.LeadId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.Agente)
             .WithMany()
             .HasForeignKey(r => r.AgenteId)
             .OnDelete(DeleteBehavior.SetNull);
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

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.Entity<ClausulaContrato>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Numero).IsRequired().HasMaxLength(50);
            e.Property(c => c.Titulo).IsRequired().HasMaxLength(200);
            e.Property(c => c.Texto).IsRequired().HasMaxLength(5000);
            e.HasData(
                new ClausulaContrato { Id = 1,  Orden = 1,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "PRIMERA",          Titulo = "PARTES",
                    Texto = "Entre {locador}, en adelante \"EL/LA LOCADOR/A\", con domicilio en {locadorDomicilio}, y {locatario}, en adelante \"EL/LA LOCATARIO/A\", con domicilio en {propiedadDireccion}, convienen celebrar el presente contrato de locación, que se regirá por el Código Civil y Comercial de la Nación (CCyCN) y la Ley N° 27551 y sus modificatorias." },
                new ClausulaContrato { Id = 2,  Orden = 2,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "SEGUNDA",          Titulo = "OBJETO",
                    Texto = "EL/LA LOCADOR/A cede en locación a EL/LA LOCATARIO/A, que acepta, el inmueble sito en {propiedadDireccion}. El inmueble tendrá por destino la vivienda familiar de EL/LA LOCATARIO/A, no pudiendo modificarlo salvo consentimiento expreso de EL/LA LOCADOR/A (art. 1196, CCyCN)." },
                new ClausulaContrato { Id = 3,  Orden = 3,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "TERCERA",          Titulo = "PLAZO",
                    Texto = "Las partes convienen que la presente locación se extenderá por {duracionMeses} MESES, desde el día {fechaInicio} hasta el día {fechaFin}, inclusive (art. 1198, CCyCN)." },
                new ClausulaContrato { Id = 4,  Orden = 4,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "CUARTA",           Titulo = "PRECIO",
                    Texto = "Por la locación, las partes convienen un canon locativo de {montoAlquiler} por mes para el período inicial del contrato." },
                new ClausulaContrato { Id = 5,  Orden = 5,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "QUINTA",           Titulo = "AJUSTE",
                    Texto = "El canon mensual definido en la cláusula anterior se actualizará {ajusteTexto}, {periodicidad}. EL/LA LOCADOR/A informará el nuevo valor al LOCATARIO/A por vía electrónica, al menos diez (10) días antes que venza el pago del mes (art. 14, Ley N° 27737)." },
                new ClausulaContrato { Id = 6,  Orden = 6,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "SEXTA",            Titulo = "PERÍODO Y LUGAR DE PAGO",
                    Texto = "EL/LA LOCATARIO/A se obliga a abonar el alquiler convenido por mes entero y adelantado{diaVencimiento}. {pagoMedio}" },
                new ClausulaContrato { Id = 7,  Orden = 7,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "SÉPTIMA",          Titulo = "DEMORA",
                    Texto = "La mora en el pago del alquiler se producirá de forma automática. Por ésta se abonará la tasa activa por plazo fijo del Banco de la Nación Argentina, durante el tiempo que demore en efectivizar el pago de los alquileres adeudados." },
                new ClausulaContrato { Id = 8,  Orden = 8,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "OCTAVA",           Titulo = "EXPENSAS, SERVICIOS E IMPUESTOS",
                    Texto = "EL/LA LOCATARIO/A tiene a su cargo el pago en tiempo y forma de: (i) servicios de energía eléctrica, agua y gas; (ii) cargas y contribuciones asociadas al destino de vivienda del inmueble; (iii) las expensas que deriven de gastos habituales ordinarios. EL/LA LOCADOR/A tiene a su cargo las cargas y contribuciones que graven el inmueble (impuesto inmobiliario) y las expensas comunes extraordinarias (art. 1209, CCyCN)." },
                new ClausulaContrato { Id = 9,  Orden = 9,  TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "NOVENA",           Titulo = "TITULARIDAD DE SERVICIOS",
                    Texto = "EL/LA LOCATARIO/A, dentro de los TREINTA (30) días de suscripto el presente, transferirá a su nombre los servicios públicos, TV por cable e internet. EL/LA LOCADOR/A, dentro de los TREINTA (30) días de terminado el contrato, asegurará el cambio de titularidad del total de servicios." },
                new ClausulaContrato { Id = 10, Orden = 10, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA",           Titulo = "REGLAMENTOS Y CONSORCIO",
                    Texto = "EL/LA LOCATARIO/A se compromete a respetar los reglamentos de Copropiedad y Administración y el Interno del edificio, siendo responsable ante el consorcio de propietarios de las transgresiones estipuladas en los mismos." },
                new ClausulaContrato { Id = 11, Orden = 11, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA PRIMERA",   Titulo = "MEJORAS Y MODIFICACIONES",
                    Texto = "EL/LA LOCATARIO/A no podrá hacer modificaciones de ninguna naturaleza en la propiedad, sin consentimiento previo del/la LOCADOR/A expresado por vía electrónica. En caso de que las modificaciones impliquen mejoras del inmueble, EL/LA LOCADOR/A deberá reembolsar al LOCATARIO/A lo invertido." },
                new ClausulaContrato { Id = 12, Orden = 12, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA SEGUNDA",   Titulo = "PROHIBICIÓN",
                    Texto = "El presente contrato de locación es intransferible. Queda prohibido al/la LOCATARIO/A ceder o subarrendar total o parcialmente el inmueble sin consentimiento del/la LOCADOR/A. Asimismo, queda prohibido usarlo contrariando las leyes o darle otro destino que el de vivienda familiar." },
                new ClausulaContrato { Id = 13, Orden = 13, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA TERCERA",   Titulo = "RESPONSABILIDADES",
                    Texto = "EL/LA LOCATARIO/A tiene la obligación de mantener el inmueble y restituirlo en el estado que lo recibió, excepto por deterioros ocasionados por el mero transcurso del tiempo y por el uso regular (art. 1210 CCyCN). EL/LA LOCADOR/A debe entregarlo en las condiciones previstas, conservarlo para que sirva al uso convenido y efectuar las reparaciones que exija el deterioro originado por causa no imputable al LOCATARIO/A (art. 1201, CCyCN)." },
                new ClausulaContrato { Id = 14, Orden = 14, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA CUARTA",    Titulo = "REPARACIONES",
                    Texto = "En caso de negativa o silencio del/la LOCADOR/A ante un reclamo debidamente notificado para efectuar una reparación urgente, EL/LA LOCATARIO/A puede realizarla por sí, con cargo al/la LOCADOR/A, una vez transcurridas al menos veinticuatro (24) horas corridas. Si las reparaciones no fueran urgentes, EL/LA LOCATARIO/A debe intimar al/la LOCADOR/A con un plazo mínimo de diez (10) días (art. 1201, CCyCN)." },
                new ClausulaContrato { Id = 15, Orden = 15, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA QUINTA",    Titulo = "PRIMER MES",
                    Texto = "EL/LA LOCATARIO/A abona en este acto la cantidad de {montoAlquiler} en concepto del alquiler correspondiente al mes de {mesInicio}. Por este primer canon, EL/LA LOCADOR/A remitirá la correspondiente factura electrónica conforme la cláusula sexta del presente." },
                new ClausulaContrato { Id = 16, Orden = 16, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA SEXTA",     Titulo = "DEPÓSITO EN GARANTÍA",
                    Texto = "En garantía de las obligaciones contraídas, EL/LA LOCATARIO/A da en depósito al/la LOCADOR/A la suma de {montoAlquiler}, equivalente al valor del primer mes de alquiler del contrato. Al momento de restitución del inmueble, EL/LA LOCADOR/A deberá devolver el depósito actualizado al valor del último mes del contrato (art. 1196, CCyCN)." },
                new ClausulaContrato { Id = 17, Orden = 17, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA SÉPTIMA",   Titulo = "FINALIZACIÓN",
                    Texto = "La finalización del presente contrato, por cualquier modalidad de extinción, se formalizará a través del Acta de Entrega de Llaves, que EL/LA LOCADOR/A confeccionará y cuyo texto enviará al/la LOCATARIO/A 48 horas antes de la entrega. El acta informará la fecha y hora de entrega, el estado del inmueble, el estado de las obligaciones contractuales y la devolución total o parcial del depósito en garantía." },
                new ClausulaContrato { Id = 18, Orden = 18, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA OCTAVA",    Titulo = "FIANZA",
                    Texto = "{garanteTexto}" },
                new ClausulaContrato { Id = 19, Orden = 19, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "DÉCIMA NOVENA",    Titulo = "RESOLUCIÓN ANTICIPADA",
                    Texto = "EL/LA LOCATARIO/A puede rescindir el presente contrato sin expresión de causa de forma anticipada una vez transcurridos los primeros seis (6) meses, notificando su decisión con un (1) mes de anticipación. Si la rescisión es en el primer año, corresponde una indemnización de un mes y medio de alquiler; después del primer año, de un mes. Si la notificación se efectúa con tres (3) meses o más de anticipación, no corresponde indemnización (art. 1221 CCyCN)." },
                new ClausulaContrato { Id = 20, Orden = 20, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "VIGÉSIMA",         Titulo = "RENOVACIÓN",
                    Texto = "Dentro de los últimos tres (3) meses del contrato, cualquiera de las partes puede convocar a la otra a conversar sobre la renovación de la locación mediante notificación fehaciente. El silencio o negativa del/la LOCADOR/A a renovar habilitará al/la LOCATARIO/A a rescindir sin preaviso ni indemnización (art. 1221 bis CCyCN)." },
                new ClausulaContrato { Id = 21, Orden = 21, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "VIGÉSIMA PRIMERA", Titulo = "FALTA DE PAGO",
                    Texto = "La falta de pago de dos (2) meses de alquiler consecutivos da derecho al/la LOCADOR/A a considerar irrevocablemente rescindido el contrato y tramitar la acción de desalojo. Previo a ello, EL/LA LOCADOR/A deberá intimar fehacientemente al/la LOCATARIO/A, otorgando un plazo no inferior a diez (10) días (art. 1222 CCyCN)." },
                new ClausulaContrato { Id = 22, Orden = 22, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "VIGÉSIMA SEGUNDA", Titulo = "DOMICILIOS",
                    Texto = "Las partes establecen los siguientes domicilios: a) LOCADOR/A: {locadorDomicilio}; {locadorEmail}. b) LOCATARIO/A: en el inmueble locado ({propiedadDireccion}); {locatarioEmail}. Ambas convienen que las comunicaciones entre sí se efectuarán por vía electrónica, las que se tendrán por válidas y plenamente eficaces (art. 75, CCyCN)." },
                new ClausulaContrato { Id = 23, Orden = 23, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "VIGÉSIMA TERCERA", Titulo = "DIÁLOGO",
                    Texto = "Las partes se comprometen a manejarse en todo momento de buena fe y a sostener diálogo permanente, pacífico y tolerante. Ante desavenencias, se comprometen a recurrir a mediación comunitaria gratuita en la Defensoría del Pueblo." },
                new ClausulaContrato { Id = 24, Orden = 24, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "VIGÉSIMA CUARTA",  Titulo = "JURISDICCIÓN",
                    Texto = "Las partes se someten a la jurisdicción de los Tribunales Ordinarios de la ciudad de {ciudad}, con renuncia expresa a cualquier otro fuero o jurisdicción." },
                new ClausulaContrato { Id = 25, Orden = 25, TenantId = 1, Activo = true, FechaCreacion = seedDate, FechaActualizacion = seedDate,
                    Numero = "VIGÉSIMA QUINTA",  Titulo = "REGISTRACIÓN",
                    Texto = "En cumplimiento de la normativa vigente, EL/LA LOCADOR/A registrará el presente contrato ante la AFIP dentro de los próximos treinta (30) días de suscripto." }
            );
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

        builder.Entity<Contrato>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Codigo).IsRequired().HasMaxLength(20);
            e.HasIndex(c => c.Codigo).IsUnique();
            e.HasIndex(c => c.PropietarioRefId);
            e.HasIndex(c => c.InquilinoRefId);
            e.Property(c => c.LocadorNombre).IsRequired().HasMaxLength(100);
            e.Property(c => c.LocadorApellido).IsRequired().HasMaxLength(100);
            e.Property(c => c.LocadorDni).HasMaxLength(20);
            e.Property(c => c.LocadorEmail).HasMaxLength(200);
            e.Property(c => c.LocadorTelefono).HasMaxLength(50);
            e.Property(c => c.LocadorDomicilio).HasMaxLength(300);
            e.Property(c => c.LocadorBanco).HasMaxLength(100);
            e.Property(c => c.LocadorCbu).HasMaxLength(50);
            e.Property(c => c.LocadorCuit).HasMaxLength(20);
            e.Property(c => c.LocatarioNombre).IsRequired().HasMaxLength(100);
            e.Property(c => c.LocatarioApellido).IsRequired().HasMaxLength(100);
            e.Property(c => c.LocatarioDni).HasMaxLength(20);
            e.Property(c => c.LocatarioEmail).HasMaxLength(200);
            e.Property(c => c.LocatarioTelefono).HasMaxLength(50);
            e.Property(c => c.GaranteNombre).HasMaxLength(100);
            e.Property(c => c.GaranteApellido).HasMaxLength(100);
            e.Property(c => c.GaranteDni).HasMaxLength(20);
            e.Property(c => c.GaranteTelefono).HasMaxLength(50);
            e.Property(c => c.MontoBase).HasColumnType("decimal(18,2)");
            e.Property(c => c.ComisionLocadorPorcentaje).HasColumnType("decimal(5,2)");
            e.Property(c => c.ComisionLocadorMonto).HasColumnType("decimal(18,2)");
            e.Property(c => c.ComisionLocatarioPorcentaje).HasColumnType("decimal(5,2)");
            e.Property(c => c.ComisionLocatarioMonto).HasColumnType("decimal(18,2)");
            e.Property(c => c.Observaciones).HasMaxLength(2000);
            e.Property(c => c.ArchivoUrl).HasMaxLength(500);
            e.HasOne(c => c.Propiedad)
             .WithMany()
             .HasForeignKey(c => c.PropiedadId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Reserva)
             .WithMany()
             .HasForeignKey(c => c.ReservaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Agente)
             .WithMany()
             .HasForeignKey(c => c.AgenteId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Pago>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.ContratoId, p.NumeroCuota }).IsUnique();
            e.Property(p => p.MontoEsperado).HasColumnType("decimal(18,2)");
            e.Property(p => p.MontoPagado).HasColumnType("decimal(18,2)");
            e.Property(p => p.Observaciones).HasMaxLength(1000);
            e.HasOne(p => p.Contrato)
             .WithMany(c => c.Pagos)
             .HasForeignKey(p => p.ContratoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PagoDetalle>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Monto).HasColumnType("decimal(18,2)");
            e.Property(d => d.Referencia).HasMaxLength(200);
            e.Property(d => d.ChequeBanco).HasMaxLength(100);
            e.Property(d => d.ChequeNumero).HasMaxLength(50);
            e.HasOne(d => d.Pago)
             .WithMany(p => p.Detalles)
             .HasForeignKey(d => d.PagoId)
             .OnDelete(DeleteBehavior.Cascade);
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
