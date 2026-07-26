using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
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
}
