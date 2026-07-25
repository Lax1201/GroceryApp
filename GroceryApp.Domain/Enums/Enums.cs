namespace GroceryApp.Domain.Enums;

public enum RolEmpleado
{
    EmpleadoSucursal,
    Repartidor,
    Admin
}

public enum TipoZona
{
    CascoUrbano,
    MunicipioAledano
}

public enum EstadoPedido
{
    Pendiente,
    Confirmado,
    EnPreparacion,
    Listo,
    EnCamino,
    Entregado,
    NoEntregado,
    Cancelado,
    Rechazado
}

public enum EstadoEntrega
{
    Asignado,
    EnCamino,
    Entregado,
    NoEntregado
}
