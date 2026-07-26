using System.ComponentModel.DataAnnotations;

namespace GroceryApp.Api.Contracts;

public record CrearDireccionRequest(
    double Latitud,
    double Longitud,
    [Required, MaxLength(300)] string Referencia,
    bool EsPrincipal
);

public record ActualizarDireccionRequest(
    double Latitud,
    double Longitud,
    [Required, MaxLength(300)] string Referencia,
    bool EsPrincipal
);
