using System.Security.Claims;
using System.Threading.RateLimiting;
using GroceryApp.Application.Common;
using GroceryApp.Application.Security;
using GroceryApp.Application.Services;
using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using GroceryApp.Infrastructure.Security;
using GroceryApp.Panel.Auth;
using GroceryApp.Panel.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Base de datos ---
// En Development se aceptan los placeholders de appsettings.json; en cualquier otro
// entorno la configuración debe provenir de variables de entorno/configuración externa.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
ConfiguracionSegura.ValidarCadenaConexion(connectionString, builder.Environment.IsDevelopment());

builder.Services.AddDbContext<GroceryAppDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Abstracciones y servicios de backend reutilizados ---
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<GroceryAppDbContext>());
builder.Services.AddScoped<PasswordHasher<Empleado>>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Servicios de aplicación
builder.Services.AddScoped<EmpleadoAuthService>();
builder.Services.AddScoped<SucursalService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<CatalogoAdminService>();
builder.Services.AddScoped<IEstrategiaAsignacion, EstrategiaAsignacionCargaSimple>();
builder.Services.AddScoped<EntregaService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ZonaResolverService>();
builder.Services.AddScoped<DireccionService>();

// --- Autenticación y Autorización por Cookies ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "GroceryApp.Panel.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acceso-denegado";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// --- Rate limiting del login por cookies: límite por IP contra fuerza bruta ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Respuesta 429 real con un mensaje comprensible. Escribir el body inicia la
    // respuesta, así el middleware respeta este status en vez de dejarla vacía.
    options.OnRejected = async (context, token) =>
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "text/html; charset=utf-8";
        await response.WriteAsync(
            "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">" +
            "<title>Demasiados intentos</title></head>" +
            "<body style=\"font-family:sans-serif;text-align:center;padding:3rem;\">" +
            "<h1>Demasiados intentos</h1>" +
            "<p>Esperá un minuto e intentá de nuevo.</p>" +
            "<p><a href=\"/login\">Volver al inicio de sesión</a></p>" +
            "</body></html>",
            token);
    };

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// --- Blazor Server Components ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();

// --- Endpoints HTTP para inicio y cierre de sesión (Cookies) ---
app.MapPost("/auth/login", async (
    HttpContext httpContext,
    [FromForm] string usuario,
    [FromForm] string password,
    EmpleadoAuthService authService) =>
{
    if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=Por+favor+ingrese+usuario+y+contrase%C3%B1a.");
    }

    var validacion = await authService.ValidarCredencialesAsync(usuario.Trim(), password);
    if (!validacion.EsExitoso)
    {
        var mensajeError = Uri.EscapeDataString(validacion.Error ?? "Usuario o contraseña incorrectos.");
        return Results.Redirect($"/login?error={mensajeError}");
    }

    var empleado = validacion.Valor!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
        new(ClaimTypes.Name, empleado.Nombre),
        new(ClaimTypes.Role, empleado.Rol.ToString()),
        new("usuario", empleado.Usuario)
    };

    if (empleado.SucursalId.HasValue)
    {
        claims.Add(new Claim("sucursalId", empleado.SucursalId.Value.ToString()));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    var authProperties = new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
    };

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

    // Redirección por rol según hoja de ruta
    var destino = empleado.Rol switch
    {
        RolEmpleado.Admin => "/admin/catalogo",
        RolEmpleado.EmpleadoSucursal => "/sucursal/pedidos",
        RolEmpleado.Repartidor => "/repartidor/entregas",
        _ => "/"
    };

    return Results.Redirect(destino);
}).DisableAntiforgery().RequireRateLimiting("LoginPolicy");

app.MapGet("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapPost("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
