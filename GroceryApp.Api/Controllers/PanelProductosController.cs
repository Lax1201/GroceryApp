using Asp.Versioning;
using GroceryApp.Api.Contracts;
using GroceryApp.Application.Dtos;
using GroceryApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/productos")]
[Authorize(Roles = "Admin")]
public class PanelProductosController : ControllerBase
{
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly CatalogoAdminService _catalogo;
    private readonly IWebHostEnvironment _env;

    public PanelProductosController(CatalogoAdminService catalogo, IWebHostEnvironment env)
    {
        _catalogo = catalogo;
        _env = env;
    }

    [HttpGet("sucursal/{sucursalId:int}")]
    public async Task<ActionResult<List<ProductoAdminDto>>> ListarPorSucursal(int sucursalId, CancellationToken ct)
        => Ok(await _catalogo.ListarPorSucursalAsync(sucursalId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(CrearProductoRequest request, CancellationToken ct)
    {
        var resultado = await _catalogo.CrearAsync(
            request.Nombre, request.Descripcion, request.CategoriaId, request.SucursalId, request.Precio, request.StockDisponible, ct);

        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);

        return CreatedAtAction(nameof(ListarPorSucursal), new { sucursalId = request.SucursalId }, new { id = resultado.Valor });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, ActualizarProductoRequest request, CancellationToken ct)
    {
        var resultado = await _catalogo.ActualizarAsync(id, request.Nombre, request.Descripcion, request.CategoriaId, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);

        return NoContent();
    }

    [HttpPut("{id:int}/sucursal/{sucursalId:int}")]
    public async Task<IActionResult> ActualizarPrecioStock(int id, int sucursalId, ActualizarPrecioStockRequest request, CancellationToken ct)
    {
        var resultado = await _catalogo.ActualizarPrecioStockAsync(id, sucursalId, request.Precio, request.StockDisponible, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);

        return NoContent();
    }

    /// <summary>Sube el archivo real a wwwroot/uploads/productos y guarda la URL relativa en Producto.FotoUrl.</summary>
    [HttpPost("{id:int}/foto")]
    [RequestSizeLimit(5_000_000)] // 5 MB
    public async Task<IActionResult> SubirFoto(int id, IFormFile archivo, CancellationToken ct)
    {
        if (archivo.Length == 0)
            return Problem(detail: "El archivo está vacío.", statusCode: StatusCodes.Status400BadRequest);

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            return Problem(detail: "Solo se permiten imágenes JPG, PNG o WEBP.", statusCode: StatusCodes.Status400BadRequest);

        var carpeta = Path.Combine(_env.WebRootPath, "uploads", "productos");
        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"{id}-{Guid.NewGuid():N}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);

        await using (var stream = new FileStream(rutaFisica, FileMode.Create))
        {
            await archivo.CopyToAsync(stream, ct);
        }

        var urlPublica = $"/uploads/productos/{nombreArchivo}";
        var resultado = await _catalogo.ActualizarFotoAsync(id, urlPublica, ct);
        if (!resultado.EsExitoso)
            return Problem(detail: resultado.Error, statusCode: StatusCodes.Status400BadRequest);

        return Ok(new { fotoUrl = urlPublica });
    }
}
