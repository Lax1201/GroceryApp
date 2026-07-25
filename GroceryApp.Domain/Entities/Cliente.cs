namespace GroceryApp.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty; // UNIQUE
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public int NoShowCount { get; set; } = 0;

    public ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
