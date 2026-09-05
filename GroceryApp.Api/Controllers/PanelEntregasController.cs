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
