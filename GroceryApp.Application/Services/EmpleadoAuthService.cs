using GroceryApp.Application.Common;
using GroceryApp.Application.Security;
using GroceryApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class EmpleadoAuthService
{
    private readonly IAppDbContext _db;
    private readonly PasswordHasher<Empleado> _hasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public EmpleadoAuthService(IAppDbContext db, PasswordHasher<Empleado> hasher, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
    }

    // Nota: no hay endpoint de "registro" de empleado a propósito — las cuentas de
    // EmpleadoSucursal/Repartidor/Admin las crea un Admin desde el panel (Sprint 4),
    // no son autoservicio como el cliente.
    public async Task<Result<string>> LoginAsync(string usuario, string password, CancellationToken ct = default)
    {
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Usuario == usuario, ct);
        if (empleado is null)
            return Result<string>.Fallido("Usuario o contraseña incorrectos.");

        var resultadoVerificacion = _hasher.VerifyHashedPassword(empleado, empleado.PasswordHash, password);
        if (resultadoVerificacion == PasswordVerificationResult.Failed)
            return Result<string>.Fallido("Usuario o contraseña incorrectos.");

        var token = _tokenGenerator.GenerarTokenEmpleado(empleado);
        return Result<string>.Exitoso(token);
    }
}
