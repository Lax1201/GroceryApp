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

    public CatalogoController(CategoriaService categorias)
    {
        _categorias = categorias;
    }

    /// <summary>Público — la app del cliente lo consume sin autenticación.</summary>
    [HttpGet("categorias")]
    public async Task<ActionResult<List<CategoriaDto>>> Categorias(CancellationToken ct)
        => Ok(await _categorias.ListarAsync(ct));
}
