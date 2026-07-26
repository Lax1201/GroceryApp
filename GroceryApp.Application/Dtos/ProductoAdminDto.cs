namespace GroceryApp.Application.Dtos;

public record ProductoAdminDto(
    int Id,
    string Nombre,
    string? Descripcion,
    int CategoriaId,
    string CategoriaNombre,
    string? FotoUrl,
    decimal? Precio,
    bool? StockDisponible
);
