using GroceryApp.Application.Services;
using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryApp.Tests;

public class CatalogoClienteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GroceryAppDbContext _db;
    private readonly CatalogoService _catalogoService;

    public CatalogoClienteTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GroceryAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new GroceryAppDbContext(options);
        _db.Database.EnsureCreated();

        _catalogoService = new CatalogoService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task SeedCatalogoAsync()
    {
        var sucursal = new Sucursal
        {
            Nombre = "Sucursal Jinotepe",
            Direccion = "Centro",
            HorarioApertura = new TimeOnly(7, 0),
            HorarioCierre = new TimeOnly(21, 0)
        };
        _db.Sucursales.Add(sucursal);

        var catGranos = new Categoria { Nombre = "Granos básicos" };
        var catLacteos = new Categoria { Nombre = "Lácteos" };
        _db.Categorias.AddRange(catGranos, catLacteos);
        await _db.SaveChangesAsync();

        var arroz = new Producto { Nombre = "Arroz Faisán 1lb", Descripcion = "Arroz blanco de grano entero", CategoriaId = catGranos.Id, FotoUrl = "/uploads/arroz.jpg" };
        var frijoles = new Producto { Nombre = "Frijoles Rojos 1lb", Descripcion = "Frijoles rojos secos", CategoriaId = catGranos.Id, FotoUrl = null };
        var leche = new Producto { Nombre = "Leche Entera 1L", Descripcion = "Leche pasteurizada", CategoriaId = catLacteos.Id, FotoUrl = null };

        _db.Productos.AddRange(arroz, frijoles, leche);
        await _db.SaveChangesAsync();

        _db.ProductosSucursal.AddRange(
            new ProductoSucursal { ProductoId = arroz.Id, SucursalId = sucursal.Id, Precio = 22.50m, StockDisponible = true },
            new ProductoSucursal { ProductoId = frijoles.Id, SucursalId = sucursal.Id, Precio = 34.00m, StockDisponible = false },
            new ProductoSucursal { ProductoId = leche.Id, SucursalId = sucursal.Id, Precio = 40.00m, StockDisponible = true }
        );
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task ListarProductos_SinFiltros_RetornaTodosLosProductosDeLaSucursal()
    {
        await SeedCatalogoAsync();

        var resultado = await _catalogoService.ListarProductosAsync();

        Assert.Equal(3, resultado.Count);
        var arroz = resultado.First(p => p.Nombre.StartsWith("Arroz"));
        Assert.Equal(22.50m, arroz.Precio);
        Assert.True(arroz.StockDisponible);
        Assert.Equal("Granos básicos", arroz.CategoriaNombre);
    }

    [Fact]
    public async Task ListarProductos_FiltradoPorCategoria_RetornaSoloDeEsaCategoria()
    {
        await SeedCatalogoAsync();
        var catLacteos = await _db.Categorias.FirstAsync(c => c.Nombre == "Lácteos");

        var resultado = await _catalogoService.ListarProductosAsync(categoriaId: catLacteos.Id);

        Assert.Single(resultado);
        Assert.Equal("Leche Entera 1L", resultado[0].Nombre);
    }

    [Fact]
    public async Task ListarProductos_BusquedaPorNombre_FiltraCorrectamente()
    {
        await SeedCatalogoAsync();

        var resultado = await _catalogoService.ListarProductosAsync(busqueda: "frijol");

        Assert.Single(resultado);
        Assert.Equal("Frijoles Rojos 1lb", resultado[0].Nombre);
        Assert.False(resultado[0].StockDisponible);
    }

    [Fact]
    public async Task ObtenerProducto_Existente_RetornaDtoCorrecto()
    {
        await SeedCatalogoAsync();
        var arroz = await _db.Productos.FirstAsync(p => p.Nombre.StartsWith("Arroz"));

        var resultado = await _catalogoService.ObtenerProductoAsync(arroz.Id);

        Assert.True(resultado.EsExitoso);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(arroz.Id, resultado.Valor.Id);
        Assert.Equal("Arroz Faisán 1lb", resultado.Valor.Nombre);
        Assert.Equal(22.50m, resultado.Valor.Precio);
    }

    [Fact]
    public async Task ObtenerProducto_Inexistente_RetornaFallo()
    {
        await SeedCatalogoAsync();

        var resultado = await _catalogoService.ObtenerProductoAsync(9999);

        Assert.False(resultado.EsExitoso);
        Assert.Contains("no encontrado", resultado.Error);
    }
}
