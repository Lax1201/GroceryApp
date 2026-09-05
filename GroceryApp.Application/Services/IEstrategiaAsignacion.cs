using GroceryApp.Application.Common;

namespace GroceryApp.Application.Services;

/// <summary>
/// Abstracción para la estrategia de asignación de pedidos a repartidores.
/// Permite desacoplar el algoritmo de despacho para soportar futuras estrategias
/// (proximidad, GPS, balanceo avanzado) sin modificar el núcleo de la aplicación.
/// </summary>
public interface IEstrategiaAsignacion
{
    /// <summary>
    /// Selecciona el repartidor más elegible para una sucursal dada según la estrategia configurada.
    /// Retorna el ID del repartidor seleccionado o null si no hay repartidores disponibles.
    /// </summary>
    Task<int?> SeleccionarRepartidorAsync(int sucursalId, IAppDbContext db, CancellationToken ct = default);
}
