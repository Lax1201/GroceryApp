using System.ComponentModel.DataAnnotations;

namespace GroceryApp.Api.Contracts;

public record ItemPedidoRequest(
    [Required] int ProductoId,
    [Required, Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")] int Cantidad
);

public record CrearPedidoRequest(
    [Required] int DireccionId,
    [Required, MinLength(1, ErrorMessage = "El pedido debe tener al menos un producto.")] List<ItemPedidoRequest> Items
);

public record AsignarRepartidorRequest([Required] int RepartidorId);
