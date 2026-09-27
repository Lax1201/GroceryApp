namespace GroceryApp.Application.Common;

/// <summary>
/// Validación mínima de la firma (magic bytes) de imágenes subidas.
/// Complementa la validación de extensión: un archivo renombrado a .jpg/.png/.webp
/// no pasa esta comprobación. No inspecciona el contenido completo de la imagen.
/// </summary>
public static class ValidadorImagen
{
    /// <summary>Bytes necesarios para verificar cualquiera de las firmas soportadas (WEBP es la más larga).</summary>
    public const int LargoEncabezado = 12;

    /// <summary>
    /// Lee hasta <see cref="LargoEncabezado"/> bytes tolerando lecturas parciales del stream.
    /// Devuelve solo los bytes realmente disponibles (menos si el stream termina antes).
    /// </summary>
    public static async Task<byte[]> LeerEncabezadoAsync(Stream stream, CancellationToken ct = default)
    {
        var buffer = new byte[LargoEncabezado];
        var leidos = await stream.ReadAtLeastAsync(buffer, LargoEncabezado, throwOnEndOfStream: false, ct);
        if (leidos == buffer.Length)
            return buffer;

        var parcial = new byte[leidos];
        Array.Copy(buffer, parcial, leidos);
        return parcial;
    }

    public static bool FirmaValida(ReadOnlySpan<byte> encabezado, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" =>
                encabezado.Length >= 3 &&
                encabezado[0] == 0xFF && encabezado[1] == 0xD8 && encabezado[2] == 0xFF,
            ".png" =>
                encabezado.Length >= 8 &&
                encabezado[0] == 0x89 && encabezado[1] == 0x50 && encabezado[2] == 0x4E && encabezado[3] == 0x47 &&
                encabezado[4] == 0x0D && encabezado[5] == 0x0A && encabezado[6] == 0x1A && encabezado[7] == 0x0A,
            ".webp" =>
                encabezado.Length >= 12 &&
                encabezado[0] == 0x52 && encabezado[1] == 0x49 && encabezado[2] == 0x46 && encabezado[3] == 0x46 &&
                encabezado[8] == 0x57 && encabezado[9] == 0x45 && encabezado[10] == 0x42 && encabezado[11] == 0x50,
            _ => false
        };
    }
}
