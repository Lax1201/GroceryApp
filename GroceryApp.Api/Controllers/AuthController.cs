using Asp.Versioning;
using GroceryApp.Api.Contracts;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly ClienteAuthService _clienteAuth;
    private readonly EmpleadoAuthService _empleadoAuth;

    public AuthController(ClienteAuthService clienteAuth, EmpleadoAuthService empleadoAuth)
    {
        _clienteAuth = clienteAuth;
        _empleadoAuth = empleadoAuth;
    }

    /// <summary>Registro de un nuevo cliente (autoservicio, desde la app).</summary>
    [HttpPost("cliente/registro")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<AuthResponse>> RegistrarCliente(RegistroClienteRequest request, CancellationToken ct)
    {
        var resultado = await _clienteAuth.RegistrarAsync(request.Nombre, request.Telefono, request.Password, request.Email, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status409Conflict);

        return Ok(new AuthResponse(resultado.Valor!));
    }

    /// <summary>Login de cliente (app Android).</summary>
    [HttpPost("cliente/login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<AuthResponse>> LoginCliente(LoginClienteRequest request, CancellationToken ct)
    {
        var resultado = await _clienteAuth.LoginAsync(request.Telefono, request.Password, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status401Unauthorized);

        return Ok(new AuthResponse(resultado.Valor!));
    }

    /// <summary>
    /// Login de empleado (EmpleadoSucursal, Repartidor o Admin — el rol viene en el JWT).
    /// No hay endpoint de registro de empleado: esas cuentas las crea un Admin (Sprint 4).
    /// </summary>
    [HttpPost("empleado/login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<AuthResponse>> LoginEmpleado(LoginEmpleadoRequest request, CancellationToken ct)
    {
        var resultado = await _empleadoAuth.LoginAsync(request.Usuario, request.Password, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status401Unauthorized);

        return Ok(new AuthResponse(resultado.Valor!));
    }
}
