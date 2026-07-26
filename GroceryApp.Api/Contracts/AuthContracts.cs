using System.ComponentModel.DataAnnotations;

namespace GroceryApp.Api.Contracts;

public record RegistroClienteRequest(
    [Required, MaxLength(150)] string Nombre,
    [Required, RegularExpression(@"^(\+505)?[0-9]{8}$",
        ErrorMessage = "Formato de teléfono nicaragüense inválido (8 dígitos, con o sin +505).")]
    string Telefono,
    [Required, MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    string Password,
    [EmailAddress] string? Email
);

public record LoginClienteRequest(
    [Required] string Telefono,
    [Required] string Password
);

public record LoginEmpleadoRequest(
    [Required] string Usuario,
    [Required] string Password
);

public record AuthResponse(string Token);
