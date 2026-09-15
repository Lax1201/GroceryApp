using System.Security.Claims;
using Asp.Versioning;
using GroceryApp.Api.Contracts;
using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/pedidos")]
[Authorize(Roles = "EmpleadoSucursal,Admin")]
public class PanelPedidosController : ControllerBase
{
    private readonly PedidoService _pedidos;
    private readonly EntregaService _entregas;

    public PanelPedidosController(PedidoService pedidos, EntregaService entregas)
    {
        _pedidos = pedidos;
        _entregas = entregas;
    }

    /// <summary>Admin ve todas las sucursales; EmpleadoSucursal solo la suya (claim "sucursalId" del JWT).</summary>
    private int? SucursalScope
    {
        get
        {
            if (User.IsInRole("Admin")) return null;
            var claim = User.FindFirstValue("sucursalId");
            if (int.TryParse(claim, out var id)) return id;
            return -1; // Restringe acceso a sucursal si el claim no está presente o no es válido
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<PedidoResumenDto>>> Listar([FromQuery] string? estado, CancellationToken ct)
        => Ok(await _pedidos.ListarParaPanelAsync(SucursalScope, estado, ct));

    [HttpPut("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id, CancellationToken ct)
        => Responder(await _pedidos.ConfirmarAsync(id, SucursalScope, ct));

    [HttpPut("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, CancellationToken ct)
        => Responder(await _pedidos.RechazarAsync(id, SucursalScope, ct));

    [HttpPut("{id:int}/iniciar-preparacion")]
    public async Task<IActionResult> IniciarPreparacion(int id, CancellationToken ct)
        => Responder(await _pedidos.IniciarPreparacionAsync(id, SucursalScope, ct));

    [HttpPut("{id:int}/marcar-listo")]
    public async Task<IActionResult> MarcarListo(int id, CancellationToken ct)
        => Responder(await _pedidos.MarcarListoAsync(id, SucursalScope, ct));

    /// <summary>Fase 1: producto faltante al preparar -> se quita y se recalcula el total.</summary>
    [HttpDelete("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> EliminarItem(int id, int itemId, CancellationToken ct)
        => Responder(await _pedidos.EliminarItemAsync(id, itemId, SucursalScope, ct));

    /// <summary>Respaldo manual de asignación (empleado de sucursal o admin).</summary>
    [HttpPut("{id:int}/asignar-repartidor")]
    public async Task<IActionResult> AsignarRepartidor(int id, AsignarRepartidorRequest request, CancellationToken ct)
        => Responder(await _entregas.AsignarManualAsync(id, request.RepartidorId, SucursalScope, ct));

    private IActionResult Responder(Result resultado)
        => resultado.EsExitoso ? NoContent() : Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);
}
