using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Common;

/// <summary>
/// Abstracción mínima sobre el DbContext real (que vive en Infrastructure).
/// No es un patrón repositorio por entidad — los servicios siguen usando
/// LINQ/EF Core directamente sobre estos DbSets, solo evitamos que
/// Application dependa del proyecto Infrastructure.
/// Se le agregan más DbSets acá a medida que los servicios de próximos
/// sprints los necesiten (catálogo, pedidos, etc.).
/// </summary>
public interface IAppDbContext
{
    DbSet<Cliente> Clientes { get; }
    DbSet<Empleado> Empleados { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
