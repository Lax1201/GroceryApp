using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GroceryApp.Application.Security;
using GroceryApp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GroceryApp.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string GenerarTokenCliente(Cliente cliente)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
            new(ClaimTypes.Name, cliente.Nombre),
            new(ClaimTypes.Role, "Cliente"),
            new("telefono", cliente.Telefono)
        };

        return GenerarToken(claims);
    }

    public string GenerarTokenEmpleado(Empleado empleado)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
            new(ClaimTypes.Name, empleado.Nombre),
            new(ClaimTypes.Role, empleado.Rol.ToString())
        };

        // SucursalId solo existe para EmpleadoSucursal/Repartidor; Admin no tiene.
        // Los controllers de Sprint 3+ lo usan para filtrar pedidos por sucursal.
        if (empleado.SucursalId.HasValue)
            claims.Add(new Claim("sucursalId", empleado.SucursalId.Value.ToString()));

        return GenerarToken(claims);
    }

    private string GenerarToken(List<Claim> claims)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
        var issuer = _config["Jwt:Issuer"] ?? "GroceryApp";
        var horasExpiracion = _config.GetValue("Jwt:ExpiraHoras", 168); // 7 días por defecto

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(horasExpiracion),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
