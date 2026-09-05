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

public record PedidoPanelDto(
    int Id,
    string Estado,
    decimal Subtotal,
    decimal TarifaEnvio,
    decimal Total,
    DateTime FechaCreacion,
    string ClienteNombre,
    string DireccionReferencia,
    int SucursalId,
    string SucursalNombre,
    bool TieneEntregaAsignada,
    string? RepartidorNombre
);

public record RepartidorDto(int Id, string Nombre, string Usuario, int? SucursalId);
