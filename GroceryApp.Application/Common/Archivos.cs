namespace GroceryApp.Application.Common;

/// <summary>
/// Utilidades de archivos para los flujos de upload.
/// </summary>
public static class Archivos
{
    /// <summary>
    /// Elimina un archivo recién generado por la operación actual (best-effort).
    /// Nunca lanza: si la limpieza falla, no sustituye al error original.
    /// </summary>
    public static void EliminarSiExiste(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
        }
        catch
        {
            // Intencional: la limpieza no debe ocultar el error original.
        }
    }
}
