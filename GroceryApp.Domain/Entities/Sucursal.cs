namespace GroceryApp.Domain.Entities;

public class Sucursal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public TimeOnly HorarioApertura { get; set; }
    public TimeOnly HorarioCierre { get; set; }

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    public ICollection<ProductoSucursal> ProductosSucursal { get; set; } = new List<ProductoSucursal>();
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    /// <summary>
    /// Regla de negocio: true si la hora actual está dentro del horario de atención.
    /// Usado por el checkout para bloquear pedidos fuera de horario.
    /// </summary>
    public bool EstaAbierta(TimeOnly horaActual) => horaActual >= HorarioApertura && horaActual <= HorarioCierre;
}
