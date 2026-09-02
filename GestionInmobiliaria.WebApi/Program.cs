using System.Text;
using Microsoft.Extensions.FileProviders;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using GestionInmobiliaria.Infraestructura.Repositorios;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Infraestructura.Services;
using GestionInmobiliaria.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// CORS — localhost para desarrollo, cualquier *.vercel.app (producción + previews de Vercel) y
// cualquier origen extra que se cargue en AllowedOrigins (env var, coma-separado) para dominios
// propios futuros.
var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                var host = new Uri(origin).Host;
                return host == "localhost" || host.EndsWith(".vercel.app") || allowedOrigins.Contains(origin);
            })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Repositorios
builder.Services.AddScoped<IPropietarioRepository, PropietarioRepository>();
builder.Services.AddScoped<IInquilinoRepository, InquilinoRepository>();
builder.Services.AddScoped<IPropiedadRepository, PropiedadRepository>();
builder.Services.AddScoped<IAgenteRepository, AgenteRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<IEventoAgendaRepository, EventoAgendaRepository>();
builder.Services.AddScoped<ISolicitudTasacionRepository, SolicitudTasacionRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IContratoRepository, ContratoRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<ILiquidacionRepository, LiquidacionRepository>();
builder.Services.AddScoped<IGastoRepository, GastoRepository>();
builder.Services.AddScoped<IClausulaContratoRepository, ClausulaContratoRepository>();
builder.Services.AddScoped<IAppLogRepository, AppLogRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
// En producción los archivos van a Cloudinary (storage permanente); en desarrollo local se
// siguen guardando en disco — evita que cada dev necesite credenciales de Cloudinary para levantar
// el proyecto. Ver docs/logica-negocio.md, sección PENDIENTES GENERALES → "Desplegar el sistema".
if (builder.Environment.IsProduction())
    builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
else
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IPdfReportService, QuestPdfReportService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IWhatsAppService, TwilioWhatsAppService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddHostedService<RecordatorioVencimientoService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddHttpClient("Bcra", c => c.BaseAddress = new Uri("https://api.bcra.gob.ar/"));
builder.Services.AddHttpClient("Indec", c => c.BaseAddress = new Uri("https://apis.datos.gob.ar/"));
builder.Services.AddHttpClient("Gemini", c => c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"));
builder.Services.AddScoped<IReciboIaService, GeminiReciboIaService>();
builder.Services.AddScoped<ITasaMoratoriaService, TasaMoratoriaService>();
builder.Services.AddHostedService<TasaMoratoriaSchedulerService>();
builder.Services.AddScoped<IPunitorioService, PunitorioService>();
builder.Services.AddScoped<IIndiceIclService, IndiceIclService>();
builder.Services.AddHostedService<IndiceIclSchedulerService>();
builder.Services.AddScoped<IIndiceUvaService, IndiceUvaService>();
builder.Services.AddHostedService<IndiceUvaSchedulerService>();
builder.Services.AddScoped<IIndiceIpcService, IndiceIpcService>();
builder.Services.AddHostedService<IndiceIpcSchedulerService>();
builder.Services.AddHostedService<AjusteAutomaticoService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 210 * 1024 * 1024; // 210 MB para uploads de video
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GestionInmobiliaria API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("FrontendDev");
app.UseStaticFiles();

var logosPath = Path.Combine(builder.Environment.ContentRootPath, "Logos");
if (!Directory.Exists(logosPath)) Directory.CreateDirectory(logosPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(logosPath),
    RequestPath = "/logos"
});

var fotosPath = Path.Combine(builder.Environment.ContentRootPath, "FotosPropiedad");
if (!Directory.Exists(fotosPath)) Directory.CreateDirectory(fotosPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fotosPath),
    RequestPath = "/fotos-propiedad"
});

var videosPath = Path.Combine(builder.Environment.ContentRootPath, "VideosPropiedad");
if (!Directory.Exists(videosPath)) Directory.CreateDirectory(videosPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(videosPath),
    RequestPath = "/videos-propiedad"
});

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
