namespace GroceryApp.Domain.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public string? FotoUrl { get; set; }

    public ICollection<ProductoSucursal> ProductosSucursal { get; set; } = new List<ProductoSucursal>();
}
