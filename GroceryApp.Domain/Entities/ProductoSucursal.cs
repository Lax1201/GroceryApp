namespace GroceryApp.Domain.Entities;

/// <summary>
/// Catálogo global (Producto) vs. inventario/precio local (esta tabla).
/// Un mismo Producto puede tener precio y disponibilidad distinta por Sucursal.
/// </summary>
public class ProductoSucursal
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public decimal Precio { get; set; } // CHECK > 0 configurado en el DbContext
    public bool StockDisponible { get; set; }
}
