using GroceryApp.Application.Common;
using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GroceryApp.Application.Services;

public class ZonaResolverService
{
    private readonly IAppDbContext _db;
    private static readonly WKTReader WktReader = new();

    public ZonaResolverService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Busca, entre las zonas activas con polígono definido, cuál contiene el punto (lat/long).
    /// Devuelve null si el punto no cae en ninguna zona activa (fuera de cobertura por ahora).
    /// </summary>
    public async Task<Zona?> ResolverZonaAsync(double latitud, double longitud, CancellationToken ct = default)
    {
        var zonasConPoligono = await _db.Zonas
            .Where(z => z.Activa && z.PoligonoWkt != null)
            .ToListAsync(ct);

        // WKT usa orden (X Y) = (longitud, latitud), no (latitud, longitud) — error común.
        var punto = new Point(longitud, latitud);

        foreach (var zona in zonasConPoligono)
        {
            var poligono = WktReader.Read(zona.PoligonoWkt);
            if (poligono.Contains(punto))
                return zona;
        }

        return null;
    }
}
