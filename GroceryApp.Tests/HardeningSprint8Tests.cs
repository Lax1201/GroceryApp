using System.IdentityModel.Tokens.Jwt;
using GroceryApp.Application.Common;
using GroceryApp.Application.Services;
using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using GroceryApp.Infrastructure.Security;
using GroceryApp.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GroceryApp.Tests;

/// <summary>
/// Pruebas de regresión del hardening previo al Sprint 8:
/// separación del seed de empleados, validación de configuración de arranque,
/// coherencia de audiencia JWT, firma real de imágenes y horario de sucursal.
/// </summary>
public class HardeningSprint8Tests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GroceryAppDbContext _db;

    public HardeningSprint8Tests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GroceryAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new GroceryAppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // --- Seed ---

    [Fact]
    public async Task SeedAsync_NoCreaCuentasDeEmpleado()
    {
        await DbSeeder.SeedAsync(_db);

        Assert.Equal(0, await _db.Empleados.CountAsync());
        Assert.True(await _db.Zonas.AnyAsync());
        Assert.True(await _db.Categorias.AnyAsync());
        Assert.True(await _db.Sucursales.AnyAsync());
    }

    [Fact]
    public async Task SeedEmpleadosDesarrolloAsync_CreaLasCuentasDePrueba()
    {
        await DbSeeder.SeedAsync(_db);
        var hasher = new PasswordHasher<Empleado>();

        await DbSeeder.SeedEmpleadosDesarrolloAsync(_db, hasher);

        Assert.Equal(3, await _db.Empleados.CountAsync());
        Assert.True(await _db.Empleados.AnyAsync(e => e.Usuario == "admin" && e.Rol == RolEmpleado.Admin));

        var operador = await _db.Empleados.FirstAsync(e => e.Usuario == "operador");
        Assert.Equal(RolEmpleado.EmpleadoSucursal, operador.Rol);
        Assert.NotNull(operador.SucursalId);
    }

    // --- Configuración de arranque ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Server=localhost;Database=GroceryAppDb;User Id=sa;Password=CAMBIAR_ESTO;TrustServerCertificate=True;")]
    public void ValidarCadenaConexion_FueraDeDevelopment_RechazaValoresInseguros(string? cadena)
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionSegura.ValidarCadenaConexion(cadena, esDevelopment: false));
    }

    [Fact]
    public void ValidarCadenaConexion_Development_AceptaPlaceholder()
    {
        ConfiguracionSegura.ValidarCadenaConexion(
            "Server=localhost;Database=GroceryAppDb;User Id=sa;Password=CAMBIAR_ESTO;",
            esDevelopment: true);
    }

    [Fact]
    public void ValidarCadenaConexion_FueraDeDevelopment_AceptaValorReal()
    {
        ConfiguracionSegura.ValidarCadenaConexion(
            "Server=db;Database=GroceryAppDb;User Id=app;Password=clave-real;",
            esDevelopment: false);
    }

    [Fact]
    public void ValidarClaveJwt_FueraDeDevelopment_RechazaPlaceholderOClaveCorta()
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionSegura.ValidarClaveJwt("CAMBIAR_ESTO_por_una_clave_larga", esDevelopment: false));
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionSegura.ValidarClaveJwt("corta", esDevelopment: false));
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionSegura.ValidarClaveJwt(null, esDevelopment: false));
    }

    [Fact]
    public void ValidarClaveJwt_FueraDeDevelopment_AceptaClaveLarga()
    {
        ConfiguracionSegura.ValidarClaveJwt(new string('k', 40), esDevelopment: false);
    }

    // --- JWT: emisión coherente con la validación de audiencia ---

    [Fact]
    public void JwtTokenGenerator_EmiteLaAudienciaConfigurada()
    {
        var config = new ConfiguracionStub(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('k', 40),
            ["Jwt:Issuer"] = "GroceryApp",
            ["Jwt:Audience"] = "GroceryApp"
        });
        var generador = new JwtTokenGenerator(config);

        var token = generador.GenerarTokenCliente(new Cliente { Id = 7, Nombre = "Cliente", Telefono = "88888888" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains("GroceryApp", jwt.Audiences);
        Assert.Equal("GroceryApp", jwt.Issuer);
    }

    // --- Uploads: lectura robusta de firma ---

    [Fact]
    public async Task LeerEncabezadoAsync_ToleraLecturasParcialesDeUnByte()
    {
        byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        await using var stream = new StreamLecturasParciales(png);

        var encabezado = await ValidadorImagen.LeerEncabezadoAsync(stream);

        Assert.Equal(ValidadorImagen.LargoEncabezado, encabezado.Length);
        Assert.True(ValidadorImagen.FirmaValida(encabezado, ".png"));
    }

    [Fact]
    public async Task LeerEncabezadoAsync_StreamDemasiadoCorto_NoLanzaYDevuelveLoDisponible()
    {
        byte[] jpgCorto = { 0xFF, 0xD8, 0xFF };
        await using var stream = new StreamLecturasParciales(jpgCorto);

        var encabezado = await ValidadorImagen.LeerEncabezadoAsync(stream);

        Assert.Equal(3, encabezado.Length);
        Assert.True(ValidadorImagen.FirmaValida(encabezado, ".jpg"));
    }

    [Fact]
    public async Task LeerEncabezadoAsync_StreamVacio_DevuelveCeroBytes()
    {
        await using var stream = new StreamLecturasParciales(Array.Empty<byte>());

        var encabezado = await ValidadorImagen.LeerEncabezadoAsync(stream);

        Assert.Empty(encabezado);
        Assert.False(ValidadorImagen.FirmaValida(encabezado, ".png"));
    }

    [Fact]
    public void ValidadorImagen_DetectaFirmasValidasEInvalidas()
    {
        byte[] jpg = { 0xFF, 0xD8, 0xFF, 0xE0 };
        byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        byte[] webp = { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        byte[] html = { 0x3C, 0x68, 0x74, 0x6D, 0x6C, 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        Assert.True(ValidadorImagen.FirmaValida(jpg, ".jpg"));
        Assert.True(ValidadorImagen.FirmaValida(jpg, ".jpeg"));
        Assert.True(ValidadorImagen.FirmaValida(png, ".png"));
        Assert.True(ValidadorImagen.FirmaValida(webp, ".webp"));

        Assert.False(ValidadorImagen.FirmaValida(html, ".jpg"));
        Assert.False(ValidadorImagen.FirmaValida(html, ".png"));
        Assert.False(ValidadorImagen.FirmaValida(html, ".webp"));
        Assert.False(ValidadorImagen.FirmaValida(Array.Empty<byte>(), ".png"));
    }

    // --- Uploads: limpieza de archivos huérfanos ---

    [Fact]
    public void Archivos_EliminarSiExiste_EliminaSoloElArchivoIndicadoYSinLanzarSiNoExiste()
    {
        var objetivo = Path.Combine(Path.GetTempPath(), $"grocery-hardening-{Guid.NewGuid():N}.tmp");
        var vecino = Path.Combine(Path.GetTempPath(), $"grocery-hardening-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(objetivo, "objetivo");
        File.WriteAllText(vecino, "vecino");

        try
        {
            Archivos.EliminarSiExiste(objetivo);

            Assert.False(File.Exists(objetivo));
            Assert.True(File.Exists(vecino));

            // Idempotente: no lanza si el archivo ya no existe.
            Archivos.EliminarSiExiste(objetivo);
        }
        finally
        {
            File.Delete(vecino);
        }
    }

    // --- Sucursal: horario ---

    [Fact]
    public async Task SucursalService_HorarioAperturaNoAnteriorAlCierre_Falla()
    {
        var service = new SucursalService(_db);

        var resultado = await service.CrearAsync("Sucursal Test", "Centro", new TimeOnly(20, 0), new TimeOnly(7, 0));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(0, await _db.Sucursales.CountAsync());
    }

    [Fact]
    public async Task SucursalService_HorarioIgual_Falla()
    {
        var service = new SucursalService(_db);

        var resultado = await service.CrearAsync("Sucursal Test", "Centro", new TimeOnly(7, 0), new TimeOnly(7, 0));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(0, await _db.Sucursales.CountAsync());
    }

    [Fact]
    public async Task SucursalService_HorarioValido_SeCrea()
    {
        var service = new SucursalService(_db);

        var resultado = await service.CrearAsync("Sucursal Test", "Centro", new TimeOnly(7, 0), new TimeOnly(20, 0));

        Assert.True(resultado.EsExitoso);
        Assert.Equal(1, await _db.Sucursales.CountAsync());
    }

    /// <summary>Stream que devuelve como máximo 1 byte por lectura, para simular lecturas parciales.</summary>
    private sealed class StreamLecturasParciales : Stream
    {
        private readonly byte[] _datos;
        private int _posicion;

        public StreamLecturasParciales(byte[] datos) => _datos = datos;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _datos.Length;

        public override long Position
        {
            get => _posicion;
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.Length == 0 || _posicion >= _datos.Length)
                return ValueTask.FromResult(0);

            buffer.Span[0] = _datos[_posicion++];
            return ValueTask.FromResult(1);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0 || _posicion >= _datos.Length)
                return 0;

            buffer[offset] = _datos[_posicion++];
            return 1;
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class ConfiguracionStub : IConfiguration
    {
        private readonly Dictionary<string, string?> _valores;

        public ConfiguracionStub(Dictionary<string, string?> valores) => _valores = valores;

        public string? this[string key]
        {
            get => _valores.TryGetValue(key, out var valor) ? valor : null;
            set => _valores[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);

        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }
}
