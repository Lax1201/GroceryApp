using System.Text;

namespace GroceryApp.Infrastructure.Security;

/// <summary>
/// Validaciones de arranque para evitar que una configuración de desarrollo
/// (placeholders como CAMBIAR_ESTO) pase silenciosamente como válida en entornos
/// distintos de Development. No es un gestor de secretos: solo obliga a que cada
/// entorno provea su propia configuración externa (variables de entorno, user-secrets
/// o configuración equivalente).
/// </summary>
public static class ConfiguracionSegura
{
    private const string MarcadorPlaceholder = "CAMBIAR_ESTO";
    private const int LargoMinimoClaveJwt = 32;

    public static void ValidarCadenaConexion(string? cadenaConexion, bool esDevelopment)
    {
        if (esDevelopment)
            return;

        if (string.IsNullOrWhiteSpace(cadenaConexion))
            throw new InvalidOperationException(
                "Falta configurar ConnectionStrings:DefaultConnection para este entorno.");

        if (EsPlaceholder(cadenaConexion))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection contiene un placeholder de desarrollo (CAMBIAR_ESTO). " +
                "Configurá una cadena de conexión real mediante variables de entorno o configuración externa.");
    }

    public static void ValidarClaveJwt(string? claveJwt, bool esDevelopment)
    {
        if (esDevelopment)
            return;

        if (string.IsNullOrWhiteSpace(claveJwt))
            throw new InvalidOperationException("Falta configurar Jwt:Key para este entorno.");

        if (EsPlaceholder(claveJwt))
            throw new InvalidOperationException(
                "Jwt:Key contiene un placeholder de desarrollo (CAMBIAR_ESTO). " +
                "Configurá una clave real mediante variables de entorno o configuración externa.");

        if (Encoding.UTF8.GetByteCount(claveJwt) < LargoMinimoClaveJwt)
            throw new InvalidOperationException(
                $"Jwt:Key debe tener al menos {LargoMinimoClaveJwt} caracteres para firmar con HMAC-SHA256.");
    }

    private static bool EsPlaceholder(string valor)
        => valor.Contains(MarcadorPlaceholder, StringComparison.OrdinalIgnoreCase);
}
