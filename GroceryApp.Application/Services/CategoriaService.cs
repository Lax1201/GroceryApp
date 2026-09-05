using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class CategoriaService
{
    private readonly IAppDbContext _db;

    public CategoriaService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoriaDto>> ListarAsync(CancellationToken ct = default)
        => await _db.Categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre))
            .ToListAsync(ct);

    public async Task<Result<CategoriaDto>> CrearAsync(string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<CategoriaDto>.Fallido("El nombre de la categoría no puede estar vacío.");

        var nombreLimpio = nombre.Trim();
        var yaExiste = await _db.Categorias.AnyAsync(c => c.Nombre.ToLower() == nombreLimpio.ToLower(), ct);
        if (yaExiste)
            return Result<CategoriaDto>.Fallido("Ya existe una categoría con ese nombre.");

        var categoria = new Categoria { Nombre = nombreLimpio };
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync(ct);

        return Result<CategoriaDto>.Exitoso(new CategoriaDto(categoria.Id, categoria.Nombre));
    }
}
