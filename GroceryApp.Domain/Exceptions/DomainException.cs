namespace GroceryApp.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta una operación que viola una regla del negocio
/// (ej. una transición de estado inválida). La capa Application la atrapa
/// y la convierte en un Result.Fallido — el dominio no conoce ese patrón,
/// solo lanza la excepción.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
