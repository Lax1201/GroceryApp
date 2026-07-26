using System.Security.Claims;
using Asp.Versioning;
using GroceryApp.Api.Contracts;
using GroceryApp.Application.Dtos;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pedidos")]
[Authorize(Roles = "Cliente")]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidos;

    public PedidosController(PedidoService pedidos)
    {
        _pedidos = pedidos;
    }

    private int ClienteIdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Checkout. Valida catálogo, stock, misma sucursal y horario de atención.</summary>
    [HttpPost]
    public async Task<ActionResult<PedidoDetalleDto>> Crear(CrearPedidoRequest request, CancellationToken ct)
    {
        var items = request.Items.Select(i => new ItemSolicitado(i.ProductoId, i.Cantidad)).ToList();
        var resultado = await _pedidos.CrearAsync(ClienteIdActual, request.DireccionId, items, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status422UnprocessableEntity);

        return CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDetalleDto>> Obtener(int id, CancellationToken ct)
    {
        var resultado = await _pedidos.ObtenerAsync(ClienteIdActual, id, ct);
        if (!resultado.EsExitoso)
            return NotFound(new { error = resultado.Error });

        return Ok(resultado.Valor);
    }

    [HttpGet("historial")]
    public async Task<ActionResult<List<PedidoResumenDto>>> Historial(CancellationToken ct)
        => Ok(await _pedidos.HistorialAsync(ClienteIdActual, ct));

    /// <summary>Sprint 3 lo resuelve igual que "obtener" (mismo estado); Fase 3 lo separa en la UI, no en el backend.</summary>
    [HttpGet("{id:int}/seguimiento")]
    public async Task<ActionResult<PedidoDetalleDto>> Seguimiento(int id, CancellationToken ct)
    {
        var resultado = await _pedidos.ObtenerAsync(ClienteIdActual, id, ct);
        if (!resultado.EsExitoso)
            return NotFound(new { error = resultado.Error });

        return Ok(resultado.Valor);
    }

    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
    {
        var resultado = await _pedidos.CancelarAsync(ClienteIdActual, id, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);

        return NoContent();
    }
}
