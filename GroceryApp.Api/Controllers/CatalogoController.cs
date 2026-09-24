using Asp.Versioning;
using GroceryApp.Application.Dtos;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalogo")]
public class CatalogoController : ControllerBase
{
    private readonly CategoriaService _categorias;
    private readonly CatalogoService _catalogo;

    public CatalogoController(CategoriaService categorias, CatalogoService catalogo)
    {
        _categorias = categorias;
        _catalogo = catalogo;
    }

    /// <summary>Público — la app del cliente lo consume sin autenticación.</summary>
    [HttpGet("categorias")]
    public async Task<ActionResult<List<CategoriaDto>>> Categorias(CancellationToken ct)
        => Ok(await _categorias.ListarAsync(ct));

    /// <summary>Público — listado de productos con filtro opcional por categoría y búsqueda.</summary>
    [HttpGet("productos")]
    public async Task<ActionResult<List<ProductoClienteDto>>> Productos(
        [FromQuery] int? categoriaId,
        [FromQuery] string? busqueda,
        CancellationToken ct)
        => Ok(await _catalogo.ListarProductosAsync(categoriaId, busqueda, ct));

    /// <summary>Público — detalle de un producto para clientes.</summary>
    [HttpGet("productos/{id:int}")]
    public async Task<ActionResult<ProductoClienteDto>> Obtener(int id, CancellationToken ct)
    {
        var resultado = await _catalogo.ObtenerProductoAsync(id, ct);
        if (!resultado.EsExitoso)
            return NotFound(new { error = resultado.Error });

        return Ok(resultado.Valor);
    }
}
