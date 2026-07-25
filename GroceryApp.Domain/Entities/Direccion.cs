namespace GroceryApp.Domain.Entities;

public class Direccion
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string Referencia { get; set; } = string.Empty;

    public int ZonaId { get; set; }
    public Zona? Zona { get; set; }

    public bool EsPrincipal { get; set; }
}
