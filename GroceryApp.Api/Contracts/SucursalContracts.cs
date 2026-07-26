using System.ComponentModel.DataAnnotations;

namespace GroceryApp.Api.Contracts;

public record CrearSucursalRequest(
    [Required, MaxLength(150)] string Nombre,
    [Required, MaxLength(300)] string Direccion,
    [Required] TimeOnly HorarioApertura,
    [Required] TimeOnly HorarioCierre
);
