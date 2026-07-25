using System.Text;
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
builder.Services.AddDbContext<GroceryAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Hashing de contraseñas (Identity liviano: solo PasswordHasher<T>, sin UserManager/SignInManager) ---
// Cliente y Empleado son dos tipos de principal distintos, cada uno con su propio hasher.
builder.Services.AddScoped<PasswordHasher<Cliente>>();
builder.Services.AddScoped<PasswordHasher<Empleado>>();

// --- Application: abstracción del DbContext + generador de JWT + servicios de auth ---
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<GroceryAppDbContext>());
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ClienteAuthService>();
builder.Services.AddScoped<EmpleadoAuthService>();

// --- JWT ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings o variables de entorno.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GroceryApp";

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
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
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

// --- Rate limiting básico para /auth/login (Fase 4: obligatorio, no opcional) ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

// --- Migraciones + seed automático al arrancar (cómodo en desarrollo; en producción evaluar correrlo aparte) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GroceryAppDbContext>();
    var empleadoHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<Empleado>>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, empleadoHasher);
}

// El manejador de excepciones va primero: cualquier error más abajo en el pipeline
// termina como un ProblemDetails uniforme en vez del error crudo de ASP.NET.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
