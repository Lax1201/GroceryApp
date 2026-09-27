using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using GroceryApp.Application.Common;
using GroceryApp.Application.Security;
using GroceryApp.Application.Services;
using GroceryApp.Domain.Entities;
using GroceryApp.Infrastructure.Data;
using GroceryApp.Infrastructure.Security;
using GroceryApp.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
 

var builder = WebApplication.CreateBuilder(args);

// --- Base de datos ---
// En Development se aceptan los placeholders de appsettings.json; en cualquier otro
// entorno la configuración debe provenir de variables de entorno/configuración externa.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
ConfiguracionSegura.ValidarCadenaConexion(connectionString, builder.Environment.IsDevelopment());

builder.Services.AddDbContext<GroceryAppDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Hashing de contraseñas (Identity liviano: solo PasswordHasher<T>, sin UserManager/SignInManager) ---
// Cliente y Empleado son dos tipos de principal distintos, cada uno con su propio hasher.
builder.Services.AddScoped<PasswordHasher<Cliente>>();
builder.Services.AddScoped<PasswordHasher<Empleado>>();

// --- Application: abstracción del DbContext + generador de JWT + servicios de auth ---
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<GroceryAppDbContext>());
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ClienteAuthService>();
builder.Services.AddScoped<EmpleadoAuthService>();

// --- Sprint 2: zonas/direcciones y catálogo ---
builder.Services.AddScoped<ZonaResolverService>();
builder.Services.AddScoped<DireccionService>();
builder.Services.AddScoped<CatalogoAdminService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<SucursalService>();
builder.Services.AddScoped<CategoriaService>();

// --- Sprint 3: pedidos y entregas ---
builder.Services.AddScoped<IEstrategiaAsignacion, EstrategiaAsignacionCargaSimple>();
builder.Services.AddScoped<EntregaService>();
builder.Services.AddScoped<PedidoService>();

// --- JWT ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings o variables de entorno.");
ConfiguracionSegura.ValidarClaveJwt(jwtKey, builder.Environment.IsDevelopment());

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GroceryApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GroceryApp";

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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// --- Versionado de API: todo controller futuro usa /api/v{version}/... desde el día 1 ---
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Botón de "Authorize" en Swagger para pegar el JWT y probar endpoints protegidos
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Pegar el token JWT (sin la palabra 'Bearer')"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- Manejo global de errores: toda excepción no controlada devuelve un ProblemDetails consistente ---
builder.Services.AddProblemDetails();

// --- Rate limiting básico para /auth (Fase 4: obligatorio, no opcional) ---
// Particionado por IP: el límite es por cliente, no global, para no bloquear
// a todos los usuarios cuando alguien intenta fuerza bruta.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// --- Migraciones + seed automático al arrancar (cómodo en desarrollo; en producción evaluar correrlo aparte) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GroceryAppDbContext>();
    await db.Database.MigrateAsync();

    // Datos base (zonas, categorías, sucursal principal): todos los entornos.
    await DbSeeder.SeedAsync(db);

    // Cuentas de empleado de prueba y catálogo de productos: SOLO en Development.
    if (app.Environment.IsDevelopment())
    {
        var empleadoHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<Empleado>>();
        await DbSeeder.SeedEmpleadosDesarrolloAsync(db, empleadoHasher);
        await DbSeeder.SeedProductosDesarrolloAsync(db);
    }
}

// El manejador de excepciones va primero: cualquier error más abajo en el pipeline
// termina como un ProblemDetails uniforme en vez del error crudo de ASP.NET.
// En Development mostramos el detalle completo (stack trace) para poder debuggear;
// el ProblemDetails genérico sin detalle queda solo para producción.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
    app.UseStatusCodePages();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // sirve wwwroot/uploads/productos/... para las fotos subidas en Sprint 2
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
