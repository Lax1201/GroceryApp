using GroceryApp.Domain.Enums;

namespace GroceryApp.Domain.Entities;

public class Empleado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty; // UNIQUE
    public string PasswordHash { get; set; } = string.Empty;
    public RolEmpleado Rol { get; set; }

    // NULL únicamente si Rol == Admin
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public ICollection<Entrega> EntregasAsignadas { get; set; } = new List<Entrega>();
}
