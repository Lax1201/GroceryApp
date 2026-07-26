using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class SucursalService
{
    private readonly IAppDbContext _db;

    public SucursalService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SucursalDto>> ListarAsync(CancellationToken ct = default)
        => await _db.Sucursales
            .Select(s => new SucursalDto(s.Id, s.Nombre, s.Direccion, s.HorarioApertura, s.HorarioCierre))
            .ToListAsync(ct);

    public async Task<Result<SucursalDto>> CrearAsync(
        string nombre, string direccion, TimeOnly horarioApertura, TimeOnly horarioCierre, CancellationToken ct = default)
    {
        var sucursal = new Sucursal
        {
            Nombre = nombre,
            Direccion = direccion,
            HorarioApertura = horarioApertura,
            HorarioCierre = horarioCierre
        };

        _db.Sucursales.Add(sucursal);
        await _db.SaveChangesAsync(ct);

        return Result<SucursalDto>.Exitoso(
            new SucursalDto(sucursal.Id, sucursal.Nombre, sucursal.Direccion, sucursal.HorarioApertura, sucursal.HorarioCierre));
    }
}
