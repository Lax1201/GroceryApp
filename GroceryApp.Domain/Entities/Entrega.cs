using GroceryApp.Domain.Enums;
using GroceryApp.Domain.Exceptions;

namespace GroceryApp.Domain.Entities;

public class Entrega
{
    public int Id { get; set; }

    public int PedidoId { get; set; } // UNIQUE
    public Pedido? Pedido { get; set; }

    public int RepartidorId { get; set; }
    public Empleado? Repartidor { get; set; }

    public EstadoEntrega Estado { get; private set; } = EstadoEntrega.Asignado;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEntrega { get; private set; }

    public void MarcarEnCamino()
    {
        if (Estado != EstadoEntrega.Asignado)
            throw new DomainException($"No se puede marcar en camino una entrega en estado {Estado}.");
        Estado = EstadoEntrega.EnCamino;
    }

    public void MarcarEntregado()
    {
        if (Estado != EstadoEntrega.EnCamino)
            throw new DomainException($"No se puede marcar entregada una entrega en estado {Estado}.");
        Estado = EstadoEntrega.Entregado;
        FechaEntrega = DateTime.UtcNow;
    }

    public void MarcarNoEntregado()
    {
        if (Estado != EstadoEntrega.EnCamino)
            throw new DomainException($"No se puede marcar no-entregada una entrega en estado {Estado}.");
        Estado = EstadoEntrega.NoEntregado;
        FechaEntrega = DateTime.UtcNow;
    }
}
