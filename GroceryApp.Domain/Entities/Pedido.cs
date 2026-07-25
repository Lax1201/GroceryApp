using GroceryApp.Domain.Enums;

namespace GroceryApp.Domain.Entities;

public class Pedido
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public int DireccionId { get; set; }
    public Direccion? Direccion { get; set; }

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    public decimal TarifaEnvio { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
    public Entrega? Entrega { get; set; }

    /// <summary>
    /// Regla de Fase 1: producto no disponible al preparar -> se elimina y se recalcula el total.
    /// </summary>
    public void RecalcularTotales()
    {
        Subtotal = Items.Sum(i => i.Subtotal);
        Total = Subtotal + TarifaEnvio;
        FechaActualizacion = DateTime.UtcNow;
    }
}
