using GroceryApp.Application.Common;
using GroceryApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

/// <summary>
/// Estrategia de asignación inicial para el MVP:
/// Selecciona automáticamente al repartidor elegible de la misma sucursal
/// que tenga la menor cantidad de entregas activas (Asignado o EnCamino).
/// Entregas en estado Entregado o NoEntregado no se consideran activas.
/// </summary>
public class EstrategiaAsignacionCargaSimple : IEstrategiaAsignacion
{
    public async Task<int?> SeleccionarRepartidorAsync(int sucursalId, IAppDbContext db, CancellationToken ct = default)
    {
        var repartidores = await db.Empleados
            .Where(e => e.Rol == RolEmpleado.Repartidor && e.SucursalId == sucursalId)
            .Select(e => new
            {
                e.Id,
                CargaActiva = db.Entregas.Count(ent =>
                    ent.RepartidorId == e.Id &&
                    (ent.Estado == EstadoEntrega.Asignado || ent.Estado == EstadoEntrega.EnCamino))
            })
            .OrderBy(r => r.CargaActiva)
            .ThenBy(r => r.Id)
            .ToListAsync(ct);

        if (repartidores.Count == 0)
            return null;

        return repartidores.First().Id;
    }
}
