using System.Security.Claims;
using Asp.Versioning;
using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/entregas")]
[Authorize(Roles = "Repartidor")]
public class PanelEntregasController : ControllerBase
{
    private readonly EntregaService _entregas;

    public PanelEntregasController(EntregaService entregas)
    {
        _entregas = entregas;
    }

    private int RepartidorIdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int SucursalIdActual => int.Parse(User.FindFirstValue("sucursalId")!);

    /// <summary>Modelo de pool: pedidos "Listo" de su sucursal, sin repartidor asignado todavía.</summary>
    [HttpGet("disponibles")]
    public async Task<ActionResult<List<PedidoResumenDto>>> Disponibles(CancellationToken ct)
        => Ok(await _entregas.ListarDisponiblesAsync(SucursalIdActual, ct));

    /// <summary>El repartidor toma el pedido para sí mismo (autoservicio).</summary>
    [HttpPost("{pedidoId:int}/tomar")]
    public async Task<IActionResult> Tomar(int pedidoId, CancellationToken ct)
    {
        var resultado = await _entregas.TomarAsync(pedidoId, RepartidorIdActual, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status409Conflict);

        return NoContent();
    }

    [HttpGet("mias")]
    public async Task<ActionResult<List<EntregaDto>>> Mias(CancellationToken ct)
        => Ok(await _entregas.MisEntregasAsync(RepartidorIdActual, ct));

    [HttpPut("{id:int}/en-camino")]
    public async Task<IActionResult> EnCamino(int id, CancellationToken ct)
        => Responder(await _entregas.MarcarEnCaminoAsync(id, RepartidorIdActual, ct));

    [HttpPut("{id:int}/entregado")]
    public async Task<IActionResult> Entregado(int id, CancellationToken ct)
        => Responder(await _entregas.MarcarEntregadoAsync(id, RepartidorIdActual, ct));

    [HttpPut("{id:int}/no-entregado")]
    public async Task<IActionResult> NoEntregado(int id, CancellationToken ct)
        => Responder(await _entregas.MarcarNoEntregadoAsync(id, RepartidorIdActual, ct));

    private IActionResult Responder(Result resultado)
        => resultado.EsExitoso ? NoContent() : Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);
}
