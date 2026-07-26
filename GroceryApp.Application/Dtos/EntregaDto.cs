namespace GroceryApp.Application.Dtos;

public record EntregaDto(
    int Id,
    int PedidoId,
    string Estado,
    string DireccionReferencia,
    decimal Total,
    DateTime FechaAsignacion
);
