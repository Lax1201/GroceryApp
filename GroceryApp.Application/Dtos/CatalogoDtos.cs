namespace GroceryApp.Application.Dtos;

public record CategoriaDto(int Id, string Nombre);

public record SucursalDto(int Id, string Nombre, string Direccion, TimeOnly HorarioApertura, TimeOnly HorarioCierre);
