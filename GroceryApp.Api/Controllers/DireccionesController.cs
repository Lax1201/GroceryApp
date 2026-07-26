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
[Route("api/v{version:apiVersion}/direcciones")]
[Authorize(Roles = "Cliente")]
public class DireccionesController : ControllerBase
{
    private readonly DireccionService _direcciones;

    public DireccionesController(DireccionService direcciones)
    {
        _direcciones = direcciones;
    }

    private int ClienteIdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<DireccionDto>> Crear(CrearDireccionRequest request, CancellationToken ct)
    {
        var resultado = await _direcciones.CrearAsync(
            ClienteIdActual, request.Latitud, request.Longitud, request.Referencia, request.EsPrincipal, ct);

        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status422UnprocessableEntity);

        return Ok(resultado.Valor);
    }

    [HttpGet]
    public async Task<ActionResult<List<DireccionDto>>> Listar(CancellationToken ct)
        => Ok(await _direcciones.ListarAsync(ClienteIdActual, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DireccionDto>> Actualizar(int id, ActualizarDireccionRequest request, CancellationToken ct)
    {
        var resultado = await _direcciones.ActualizarAsync(
            ClienteIdActual, id, request.Latitud, request.Longitud, request.Referencia, request.EsPrincipal, ct);

        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status422UnprocessableEntity);

        return Ok(resultado.Valor);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var resultado = await _direcciones.EliminarAsync(ClienteIdActual, id, ct);
        if (!resultado.EsExitoso)
            return NotFound(new { error = resultado.Error });

        return NoContent();
    }
}
