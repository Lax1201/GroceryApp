using Asp.Versioning;
using GroceryApp.Api.Contracts;
using GroceryApp.Application.Dtos;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/sucursales")]
[Authorize(Roles = "Admin")]
public class PanelSucursalesController : ControllerBase
{
    private readonly SucursalService _sucursales;

    public PanelSucursalesController(SucursalService sucursales)
    {
        _sucursales = sucursales;
    }

    [HttpGet]
    public async Task<ActionResult<List<SucursalDto>>> Listar(CancellationToken ct)
        => Ok(await _sucursales.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<SucursalDto>> Crear(CrearSucursalRequest request, CancellationToken ct)
    {
        var resultado = await _sucursales.CrearAsync(
            request.Nombre, request.Direccion, request.HorarioApertura, request.HorarioCierre, ct);

        return CreatedAtAction(nameof(Listar), null, resultado.Valor);
    }
}
