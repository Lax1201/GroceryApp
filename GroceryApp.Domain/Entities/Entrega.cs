using GroceryApp.Domain.Enums;

namespace GroceryApp.Domain.Entities;

public class Entrega
{
    public int Id { get; set; }

    public int PedidoId { get; set; } // UNIQUE
    public Pedido? Pedido { get; set; }

    public int RepartidorId { get; set; }
    public Empleado? Repartidor { get; set; }

    public EstadoEntrega Estado { get; set; } = EstadoEntrega.Asignado;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEntrega { get; set; }
}
