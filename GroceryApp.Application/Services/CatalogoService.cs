using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class CatalogoService
{
    private readonly IAppDbContext _db;

    public CatalogoService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductoClienteDto>> ListarProductosAsync(
        int? categoriaId = null,
        string? busqueda = null,
        CancellationToken ct = default)
    {
        var sucursalId = await _db.Sucursales
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (sucursalId == 0)
            return new List<ProductoClienteDto>();

        var query = _db.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.ProductosSucursal)
            .Where(p => p.ProductosSucursal.Any(ps => ps.SucursalId == sucursalId));

        if (categoriaId.HasValue && categoriaId.Value > 0)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var busquedaNormalizada = busqueda.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        return await query
            .OrderBy(p => p.Nombre)
            .Select(p => new ProductoClienteDto(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.CategoriaId,
                p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                p.FotoUrl,
                p.ProductosSucursal
                    .Where(ps => ps.SucursalId == sucursalId)
                    .Select(ps => ps.Precio)
                    .FirstOrDefault(),
                p.ProductosSucursal
                    .Where(ps => ps.SucursalId == sucursalId)
                    .Select(ps => ps.StockDisponible)
                    .FirstOrDefault()
            ))
            .ToListAsync(ct);
    }

    public async Task<Result<ProductoClienteDto>> ObtenerProductoAsync(int id, CancellationToken ct = default)
    {
        var sucursalId = await _db.Sucursales
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (sucursalId == 0)
            return Result<ProductoClienteDto>.Fallido("No hay sucursales disponibles.");

        var producto = await _db.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.ProductosSucursal)
            .Where(p => p.Id == id && p.ProductosSucursal.Any(ps => ps.SucursalId == sucursalId))
            .Select(p => new ProductoClienteDto(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.CategoriaId,
                p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                p.FotoUrl,
                p.ProductosSucursal
                    .Where(ps => ps.SucursalId == sucursalId)
                    .Select(ps => ps.Precio)
                    .FirstOrDefault(),
                p.ProductosSucursal
                    .Where(ps => ps.SucursalId == sucursalId)
                    .Select(ps => ps.StockDisponible)
                    .FirstOrDefault()
            ))
            .FirstOrDefaultAsync(ct);

        if (producto is null)
            return Result<ProductoClienteDto>.Fallido("Producto no encontrado.");

        return Result<ProductoClienteDto>.Exitoso(producto);
    }
}
