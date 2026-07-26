namespace GroceryApp.Application.Dtos;

public record DireccionDto(
    int Id,
    double Latitud,
    double Longitud,
    string Referencia,
    bool EsPrincipal,
    string ZonaNombre,
    decimal TarifaEnvio
);
