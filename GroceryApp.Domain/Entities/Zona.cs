using GroceryApp.Domain.Enums;

namespace GroceryApp.Domain.Entities;

public class Zona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoZona Tipo { get; set; }
    public decimal TarifaEnvio { get; set; }
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Borde geográfico de la zona en formato WKT (Well-Known Text), ej:
    /// "POLYGON((-86.35 11.87, -86.34 11.87, -86.34 11.86, -86.35 11.86, -86.35 11.87))".
    /// Null hasta que se defina el polígono real (ver README de Sprint 2).
    /// </summary>
    public string? PoligonoWkt { get; set; }

    public ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
}
