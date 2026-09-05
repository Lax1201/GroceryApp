namespace GroceryApp.Application.Dtos;

public record EntregaDto(
    int Id,
    int PedidoId,
    string Estado,
    string DireccionReferencia,
    decimal Total,
    DateTime FechaAsignacion,
    string? ClienteNombre = null,
    string? ClienteTelefono = null
);

public record PedidoPoolDto(
    int Id,
    string Estado,
    decimal Total,
    DateTime FechaCreacion,
    string ClienteNombre,
    string DireccionReferencia,
    int SucursalId
);
