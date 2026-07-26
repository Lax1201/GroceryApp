using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class DireccionService
{
    private readonly IAppDbContext _db;
    private readonly ZonaResolverService _zonaResolver;

    public DireccionService(IAppDbContext db, ZonaResolverService zonaResolver)
    {
        _db = db;
        _zonaResolver = zonaResolver;
    }

    public async Task<Result<DireccionDto>> CrearAsync(
        int clienteId, double latitud, double longitud, string referencia, bool esPrincipal, CancellationToken ct = default)
    {
        var zona = await _zonaResolver.ResolverZonaAsync(latitud, longitud, ct);
        if (zona is null)
            return Result<DireccionDto>.Fallido("Esa ubicación está fuera de nuestra zona de cobertura por ahora.");

        if (esPrincipal)
            await QuitarPrincipalDeOtrasAsync(clienteId, direccionAExcluirId: null, ct);

        var direccion = new Direccion
        {
            ClienteId = clienteId,
            Latitud = latitud,
            Longitud = longitud,
            Referencia = referencia,
            ZonaId = zona.Id,
            EsPrincipal = esPrincipal
        };

        _db.Direcciones.Add(direccion);
        await _db.SaveChangesAsync(ct);

        return Result<DireccionDto>.Exitoso(
            new DireccionDto(direccion.Id, latitud, longitud, referencia, esPrincipal, zona.Nombre, zona.TarifaEnvio));
    }

    public async Task<List<DireccionDto>> ListarAsync(int clienteId, CancellationToken ct = default)
    {
        return await _db.Direcciones
            .Where(d => d.ClienteId == clienteId)
            .Include(d => d.Zona)
            .OrderByDescending(d => d.EsPrincipal)
            .Select(d => new DireccionDto(d.Id, d.Latitud, d.Longitud, d.Referencia, d.EsPrincipal, d.Zona!.Nombre, d.Zona.TarifaEnvio))
            .ToListAsync(ct);
    }

    public async Task<Result<DireccionDto>> ActualizarAsync(
        int clienteId, int direccionId, double latitud, double longitud, string referencia, bool esPrincipal, CancellationToken ct = default)
    {
        var direccion = await _db.Direcciones.FirstOrDefaultAsync(d => d.Id == direccionId && d.ClienteId == clienteId, ct);
        if (direccion is null)
            return Result<DireccionDto>.Fallido("Dirección no encontrada.");

        var zona = await _zonaResolver.ResolverZonaAsync(latitud, longitud, ct);
        if (zona is null)
            return Result<DireccionDto>.Fallido("Esa ubicación está fuera de nuestra zona de cobertura por ahora.");

        if (esPrincipal)
            await QuitarPrincipalDeOtrasAsync(clienteId, direccionAExcluirId: direccionId, ct);

        direccion.Latitud = latitud;
        direccion.Longitud = longitud;
        direccion.Referencia = referencia;
        direccion.ZonaId = zona.Id;
        direccion.EsPrincipal = esPrincipal;

        await _db.SaveChangesAsync(ct);

        return Result<DireccionDto>.Exitoso(
            new DireccionDto(direccion.Id, latitud, longitud, referencia, esPrincipal, zona.Nombre, zona.TarifaEnvio));
    }

    public async Task<Result> EliminarAsync(int clienteId, int direccionId, CancellationToken ct = default)
    {
        var direccion = await _db.Direcciones.FirstOrDefaultAsync(d => d.Id == direccionId && d.ClienteId == clienteId, ct);
        if (direccion is null)
            return Result.Fallido("Dirección no encontrada.");

        _db.Direcciones.Remove(direccion);
        await _db.SaveChangesAsync(ct);
        return Result.Exitoso();
    }

    private async Task QuitarPrincipalDeOtrasAsync(int clienteId, int? direccionAExcluirId, CancellationToken ct)
    {
        var query = _db.Direcciones.Where(d => d.ClienteId == clienteId && d.EsPrincipal);
        if (direccionAExcluirId.HasValue)
            query = query.Where(d => d.Id != direccionAExcluirId.Value);

        var actuales = await query.ToListAsync(ct);
        foreach (var d in actuales)
            d.EsPrincipal = false;
    }
}
