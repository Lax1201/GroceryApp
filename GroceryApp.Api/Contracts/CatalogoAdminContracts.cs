using System.ComponentModel.DataAnnotations;

namespace GroceryApp.Api.Contracts;

public record CrearProductoRequest(
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(500)] string? Descripcion,
    [Required] int CategoriaId,
    [Required] int SucursalId,
    [Required] decimal Precio,
    bool StockDisponible
);

public record ActualizarProductoRequest(
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(500)] string? Descripcion,
    [Required] int CategoriaId
);

public record ActualizarPrecioStockRequest(
    [Required] decimal Precio,
    bool StockDisponible
);
