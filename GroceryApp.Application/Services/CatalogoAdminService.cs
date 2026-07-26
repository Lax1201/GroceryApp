using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class CatalogoAdminService
{
    private readonly IAppDbContext _db;

    public CatalogoAdminService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductoAdminDto>> ListarPorSucursalAsync(int sucursalId, CancellationToken ct = default)
    {
        return await _db.Productos
            .Include(p => p.Categoria)
            .Select(p => new ProductoAdminDto(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.CategoriaId,
                p.Categoria!.Nombre,
                p.FotoUrl,
                p.ProductosSucursal.Where(ps => ps.SucursalId == sucursalId).Select(ps => (decimal?)ps.Precio).FirstOrDefault(),
                p.ProductosSucursal.Where(ps => ps.SucursalId == sucursalId).Select(ps => (bool?)ps.StockDisponible).FirstOrDefault()
            ))
            .ToListAsync(ct);
    }

    public async Task<Result<int>> CrearAsync(
        string nombre, string? descripcion, int categoriaId, int sucursalId, decimal precio, bool stockDisponible, CancellationToken ct = default)
    {
        if (precio <= 0)
            return Result<int>.Fallido("El precio debe ser mayor a cero.");

        var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == categoriaId, ct);
        if (!categoriaExiste)
            return Result<int>.Fallido("La categoría indicada no existe.");

        var sucursalExiste = await _db.Sucursales.AnyAsync(s => s.Id == sucursalId, ct);
        if (!sucursalExiste)
            return Result<int>.Fallido("La sucursal indicada no existe.");

        var producto = new Producto { Nombre = nombre, Descripcion = descripcion, CategoriaId = categoriaId };
        _db.Productos.Add(producto);
        await _db.SaveChangesAsync(ct); // necesitamos el Id generado antes de crear ProductoSucursal

        _db.ProductosSucursal.Add(new ProductoSucursal
        {
            ProductoId = producto.Id,
            SucursalId = sucursalId,
            Precio = precio,
            StockDisponible = stockDisponible
        });
        await _db.SaveChangesAsync(ct);

        return Result<int>.Exitoso(producto.Id);
    }

    public async Task<Result> ActualizarAsync(int productoId, string nombre, string? descripcion, int categoriaId, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == productoId, ct);
        if (producto is null)
            return Result.Fallido("Producto no encontrado.");

        var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == categoriaId, ct);
        if (!categoriaExiste)
            return Result.Fallido("La categoría indicada no existe.");

        producto.Nombre = nombre;
        producto.Descripcion = descripcion;
        producto.CategoriaId = categoriaId;

        await _db.SaveChangesAsync(ct);
        return Result.Exitoso();
    }

    /// <summary>Crea o actualiza el precio/stock de un producto en una sucursal (upsert).</summary>
    public async Task<Result> ActualizarPrecioStockAsync(int productoId, int sucursalId, decimal precio, bool stockDisponible, CancellationToken ct = default)
    {
        if (precio <= 0)
            return Result.Fallido("El precio debe ser mayor a cero.");

        var productoExiste = await _db.Productos.AnyAsync(p => p.Id == productoId, ct);
        if (!productoExiste)
            return Result.Fallido("Producto no encontrado.");

        var productoSucursal = await _db.ProductosSucursal
            .FirstOrDefaultAsync(ps => ps.ProductoId == productoId && ps.SucursalId == sucursalId, ct);

        if (productoSucursal is null)
        {
            _db.ProductosSucursal.Add(new ProductoSucursal
            {
                ProductoId = productoId,
                SucursalId = sucursalId,
                Precio = precio,
                StockDisponible = stockDisponible
            });
        }
        else
        {
            productoSucursal.Precio = precio;
            productoSucursal.StockDisponible = stockDisponible;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Exitoso();
    }

    public async Task<Result> ActualizarFotoAsync(int productoId, string fotoUrl, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == productoId, ct);
        if (producto is null)
            return Result.Fallido("Producto no encontrado.");

        producto.FotoUrl = fotoUrl;
        await _db.SaveChangesAsync(ct);
        return Result.Exitoso();
    }
}
