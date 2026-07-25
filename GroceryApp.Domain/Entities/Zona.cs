using GroceryApp.Domain.Enums;

namespace GroceryApp.Domain.Entities;

public class Zona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoZona Tipo { get; set; }
    public decimal TarifaEnvio { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
}
