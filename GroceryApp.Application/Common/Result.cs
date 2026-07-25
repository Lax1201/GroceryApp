namespace GroceryApp.Application.Common;

/// <summary>
/// Resultado estándar de un caso de uso, para no depender de excepciones
/// como control de flujo (ej: "login inválido" no es una excepción, es un resultado esperado).
/// Los servicios de Sprint 1 en adelante (AuthService, PedidoService, etc.) devuelven esto.
/// </summary>
public class Result
{
    public bool EsExitoso { get; }
    public string? Error { get; }

    protected Result(bool esExitoso, string? error)
    {
        EsExitoso = esExitoso;
        Error = error;
    }

    public static Result Exitoso() => new(true, null);
    public static Result Fallido(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Valor { get; }

    private Result(bool esExitoso, T? valor, string? error) : base(esExitoso, error)
    {
        Valor = valor;
    }

    public static Result<T> Exitoso(T valor) => new(true, valor, null);
    public static new Result<T> Fallido(string error) => new(false, default, error);
}
