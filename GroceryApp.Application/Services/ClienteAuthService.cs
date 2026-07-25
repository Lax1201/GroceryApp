using GroceryApp.Application.Common;
using GroceryApp.Application.Security;
using GroceryApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class ClienteAuthService
{
    private readonly IAppDbContext _db;
    private readonly PasswordHasher<Cliente> _hasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public ClienteAuthService(IAppDbContext db, PasswordHasher<Cliente> hasher, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<string>> RegistrarAsync(string nombre, string telefono, string password, string? email, CancellationToken ct = default)
    {
        var yaExiste = await _db.Clientes.AnyAsync(c => c.Telefono == telefono, ct);
        if (yaExiste)
            return Result<string>.Fallido("Ya existe una cuenta registrada con ese número de teléfono.");

        var cliente = new Cliente
        {
            Nombre = nombre,
            Telefono = telefono,
            Email = email
        };
        cliente.PasswordHash = _hasher.HashPassword(cliente, password);

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync(ct);

        var token = _tokenGenerator.GenerarTokenCliente(cliente);
        return Result<string>.Exitoso(token);
    }

    public async Task<Result<string>> LoginAsync(string telefono, string password, CancellationToken ct = default)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Telefono == telefono, ct);
        if (cliente is null)
            return Result<string>.Fallido("Teléfono o contraseña incorrectos.");

        var resultadoVerificacion = _hasher.VerifyHashedPassword(cliente, cliente.PasswordHash, password);
        if (resultadoVerificacion == PasswordVerificationResult.Failed)
            return Result<string>.Fallido("Teléfono o contraseña incorrectos.");

        var token = _tokenGenerator.GenerarTokenCliente(cliente);
        return Result<string>.Exitoso(token);
    }
}
