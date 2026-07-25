namespace GroceryApp.Domain.Entities;

public class PedidoItem
{
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; } // CHECK > 0

    /// <summary>
    /// Precio congelado al momento de la compra. NUNCA se recalcula
    /// contra el precio actual del producto (regla de integridad de Fase 5).
    /// </summary>
    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
}
