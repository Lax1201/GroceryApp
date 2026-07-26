namespace GroceryApp.Application.Dtos;

public record ItemSolicitado(int ProductoId, int Cantidad);

public record PedidoItemDto(int Id, int ProductoId, string ProductoNombre, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record PedidoDetalleDto(
    int Id,
    string Estado,
    int SucursalId,
    string DireccionReferencia,
    decimal TarifaEnvio,
    decimal Subtotal,
    decimal Total,
    DateTime FechaCreacion,
    List<PedidoItemDto> Items
);

public record PedidoResumenDto(
    int Id,
    string Estado,
    decimal Total,
    DateTime FechaCreacion,
    string ClienteNombre
);
