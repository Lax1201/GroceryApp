using GroceryApp.Domain.Entities;

namespace GroceryApp.Application.Security;

public interface IJwtTokenGenerator
{
    string GenerarTokenCliente(Cliente cliente);
    string GenerarTokenEmpleado(Empleado empleado);
}
