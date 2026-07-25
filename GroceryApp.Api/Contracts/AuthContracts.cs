using System.ComponentModel.DataAnnotations;

namespace GroceryApp.Api.Contracts;

public record RegistroClienteRequest(
    [property: Required, MaxLength(150)] string Nombre,
    [property: Required, RegularExpression(@"^(\+505)?[0-9]{8}$",
        ErrorMessage = "Formato de teléfono nicaragüense inválido (8 dígitos, con o sin +505).")]
    string Telefono,
    [property: Required, MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    string Password,
    [property: EmailAddress] string? Email
);

public record LoginClienteRequest(
    [property: Required] string Telefono,
    [property: Required] string Password
);

public record LoginEmpleadoRequest(
    [property: Required] string Usuario,
    [property: Required] string Password
);

public record AuthResponse(string Token);
