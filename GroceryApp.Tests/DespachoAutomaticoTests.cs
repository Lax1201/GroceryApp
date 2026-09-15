using GroceryApp.Application.Common;
using GroceryApp.Application.Services;
using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryApp.Tests;

public class DespachoAutomaticoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GroceryAppDbContext _db;
    private readonly IEstrategiaAsignacion _estrategiaSimple;
    private readonly EntregaService _entregaService;
    private readonly PedidoService _pedidoService;

    public DespachoAutomaticoTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GroceryAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new GroceryAppDbContext(options);
        _db.Database.EnsureCreated();

        _estrategiaSimple = new EstrategiaAsignacionCargaSimple();
        _entregaService = new EntregaService(_db, _estrategiaSimple);
        _pedidoService = new PedidoService(_db, _entregaService);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(Sucursal sucursal, Cliente cliente, Direccion direccion)> CrearEntornoBaseAsync(string nombreSucursal = "Sucursal Central")
    {
        var sucursal = new Sucursal
        {
            Nombre = nombreSucursal,
            Direccion = "Centro",
            HorarioApertura = new TimeOnly(7, 0),
            HorarioCierre = new TimeOnly(21, 0)
        };
        _db.Sucursales.Add(sucursal);

        var zona = new Zona
        {
            Nombre = "Zona 1",
            Tipo = TipoZona.CascoUrbano,
            TarifaEnvio = 40m
        };
        _db.Zonas.Add(zona);
        await _db.SaveChangesAsync();

        var cliente = new Cliente
        {
            Nombre = "Cliente Test",
            Telefono = "8888" + Random.Shared.Next(1000, 9999),
            PasswordHash = "hash"
        };
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        var direccion = new Direccion
        {
            ClienteId = cliente.Id,
            ZonaId = zona.Id,
            Referencia = "Costado Norte"
        };
        _db.Direcciones.Add(direccion);
        await _db.SaveChangesAsync();

        return (sucursal, cliente, direccion);
    }

    private async Task<Empleado> CrearRepartidorAsync(int sucursalId, string nombre, string usuario)
    {
        var repartidor = new Empleado
        {
            Nombre = nombre,
            Usuario = usuario,
            PasswordHash = "hash",
            Rol = RolEmpleado.Repartidor,
            SucursalId = sucursalId
        };
        _db.Empleados.Add(repartidor);
        await _db.SaveChangesAsync();
        return repartidor;
    }

    private async Task<Pedido> CrearPedidoEnPreparacionAsync(int sucursalId, int clienteId, int direccionId)
    {
        var pedido = new Pedido
        {
            SucursalId = sucursalId,
            ClienteId = clienteId,
            DireccionId = direccionId,
            TarifaEnvio = 40m,
            FechaCreacion = DateTime.UtcNow
        };
        pedido.RecalcularTotales();

        pedido.Confirmar();
        pedido.IniciarPreparacion();

        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();
        return pedido;
    }

    [Fact]
    public async Task Test1_PedidoListo_ConRepartidorDisponible_SeAsignaAutomaticamente()
    {
        // Arrange
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var repartidor = await CrearRepartidorAsync(sucursal.Id, "Carlos Repartidor", "carlos1");
        var pedido = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);

        // Act: pasar a Listo mediante PedidoService
        var resultado = await _pedidoService.MarcarListoAsync(pedido.Id, sucursal.Id);

        // Assert
        Assert.True(resultado.EsExitoso);

        var pedidoActualizado = await _db.Pedidos.Include(p => p.Entrega).FirstOrDefaultAsync(p => p.Id == pedido.Id);
        Assert.NotNull(pedidoActualizado);
        Assert.Equal(EstadoPedido.Listo, pedidoActualizado.Estado);
        Assert.NotNull(pedidoActualizado.Entrega);
        Assert.Equal(repartidor.Id, pedidoActualizado.Entrega.RepartidorId);
        Assert.Equal(EstadoEntrega.Asignado, pedidoActualizado.Entrega.Estado);
    }

    [Fact]
    public async Task Test2_DosRepartidores_SeleccionaElDeMenorCargaActiva()
    {
        // Arrange
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var rep1 = await CrearRepartidorAsync(sucursal.Id, "Repartidor Uno", "rep1");
        var rep2 = await CrearRepartidorAsync(sucursal.Id, "Repartidor Dos", "rep2");

        // Asignar 1 entrega activa a rep1
        var pedidoPrevio = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        pedidoPrevio.MarcarListo();
        await _db.SaveChangesAsync();
        _db.Entregas.Add(new Entrega { PedidoId = pedidoPrevio.Id, RepartidorId = rep1.Id });
        await _db.SaveChangesAsync();

        // rep1 tiene 1 entrega activa; rep2 tiene 0 entregas activas
        var nuevoPedido = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);

        // Act
        var res = await _pedidoService.MarcarListoAsync(nuevoPedido.Id, sucursal.Id);

        // Assert
        Assert.True(res.EsExitoso);
        var entrega = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == nuevoPedido.Id);
        Assert.NotNull(entrega);
        Assert.Equal(rep2.Id, entrega.RepartidorId); // Seleccionó a rep2 por menor carga
    }

    [Fact]
    public async Task Test3_RepartidoresConDiferenteNumeroDeEntregasActivas()
    {
        // Arrange
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var repA = await CrearRepartidorAsync(sucursal.Id, "Repartidor A", "repA");
        var repB = await CrearRepartidorAsync(sucursal.Id, "Repartidor B", "repB");

        // repA tiene 2 entregas activas (1 Asignado, 1 EnCamino)
        var p1 = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        p1.MarcarListo();
        var p2 = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        p2.MarcarListo();
        await _db.SaveChangesAsync();

        var e1 = new Entrega { PedidoId = p1.Id, RepartidorId = repA.Id };
        var e2 = new Entrega { PedidoId = p2.Id, RepartidorId = repA.Id };
        e2.MarcarEnCamino();
        _db.Entregas.AddRange(e1, e2);

        // repB tiene 1 entrega activa (Asignado)
        var p3 = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        p3.MarcarListo();
        await _db.SaveChangesAsync();
        var e3 = new Entrega { PedidoId = p3.Id, RepartidorId = repB.Id };
        _db.Entregas.Add(e3);
        await _db.SaveChangesAsync();

        // Act: nuevo pedido
        var pNuevo = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        var res = await _pedidoService.MarcarListoAsync(pNuevo.Id, sucursal.Id);

        // Assert: repB tiene carga 1 vs repA carga 2 -> se asigna a repB
        Assert.True(res.EsExitoso);
        var entregaNueva = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == pNuevo.Id);
        Assert.NotNull(entregaNueva);
        Assert.Equal(repB.Id, entregaNueva.RepartidorId);
    }

    [Fact]
    public async Task Test4_SoloSeConsideranRepartidoresDeLaMismaSucursal()
    {
        // Arrange
        var (sucursal1, cliente, direccion) = await CrearEntornoBaseAsync("Sucursal Jinotepe");
        var (sucursal2, _, _) = await CrearEntornoBaseAsync("Sucursal Diriamba");

        // Sucursal 1 NO tiene repartidores; Sucursal 2 sí tiene
        var repSucursal2 = await CrearRepartidorAsync(sucursal2.Id, "Repartidor Diriamba", "diriamba_rep");

        var pedidoSuc1 = await CrearPedidoEnPreparacionAsync(sucursal1.Id, cliente.Id, direccion.Id);

        // Act
        var res = await _pedidoService.MarcarListoAsync(pedidoSuc1.Id, sucursal1.Id);

        // Assert: el pedido queda Listo pero NO se asigna al repartidor de otra sucursal
        Assert.True(res.EsExitoso);
        var pedidoActualizado = await _db.Pedidos.Include(p => p.Entrega).FirstOrDefaultAsync(p => p.Id == pedidoSuc1.Id);
        Assert.NotNull(pedidoActualizado);
        Assert.Equal(EstadoPedido.Listo, pedidoActualizado.Estado);
        Assert.Null(pedidoActualizado.Entrega); // Sin entrega asignada
    }

    [Fact]
    public async Task Test5_SinRepartidoresDisponibles_PedidoQuedaListoEnPoolSinEntrega()
    {
        // Arrange: sucursal sin repartidores
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var pedido = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);

        // Act
        var res = await _pedidoService.MarcarListoAsync(pedido.Id, sucursal.Id);

        // Assert
        Assert.True(res.EsExitoso);
        var pedidoActualizado = await _db.Pedidos.Include(p => p.Entrega).FirstOrDefaultAsync(p => p.Id == pedido.Id);
        Assert.NotNull(pedidoActualizado);
        Assert.Equal(EstadoPedido.Listo, pedidoActualizado.Estado);
        Assert.Null(pedidoActualizado.Entrega);

        // Verificar que aparece en el listado del Pool
        var pool = await _entregaService.ListarPoolDisponiblesAsync(sucursal.Id);
        Assert.Contains(pool, p => p.Id == pedido.Id);
    }

    [Fact]
    public async Task Test6_PedidoPendienteEnPool_SeAsignaCuandoRepartidorQuedaDisponible()
    {
        // Arrange
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var repartidor = await CrearRepartidorAsync(sucursal.Id, "Repartidor Unico", "unico_rep");

        // Pedido 1: asignado y en camino con el repartidor
        var p1 = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        await _pedidoService.MarcarListoAsync(p1.Id, sucursal.Id);
        var entregaP1 = await _db.Entregas.FirstAsync(e => e.PedidoId == p1.Id);
        await _entregaService.MarcarEnCaminoAsync(entregaP1.Id, repartidor.Id);

        // Pedido 2: creado en el Pool como pendiente
        var p2 = new Pedido
        {
            SucursalId = sucursal.Id,
            ClienteId = cliente.Id,
            DireccionId = direccion.Id,
            TarifaEnvio = 40m,
            FechaCreacion = DateTime.UtcNow
        };
        p2.RecalcularTotales();
        p2.Confirmar();
        p2.IniciarPreparacion();
        p2.MarcarListo();
        _db.Pedidos.Add(p2);
        await _db.SaveChangesAsync();

        // Verificar que p2 está en el pool sin entrega
        var entregaP2Antes = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == p2.Id);
        Assert.Null(entregaP2Antes);

        // Act: el repartidor entrega p1
        var resEntrega = await _entregaService.MarcarEntregadoAsync(entregaP1.Id, repartidor.Id);

        // Assert
        Assert.True(resEntrega.EsExitoso);

        // Al liberarse, ProcesarPendientesPoolAsync debió asignar p2 automáticamente al repartidor
        var entregaP2Despues = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == p2.Id);
        Assert.NotNull(entregaP2Despues);
        Assert.Equal(repartidor.Id, entregaP2Despues.RepartidorId);
        Assert.Equal(EstadoEntrega.Asignado, entregaP2Despues.Estado);
    }

    [Fact]
    public async Task Test7_EntregasEntregadoYNoEntregado_NoCuentanComoCargaActiva()
    {
        // Arrange
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var repConHistorico = await CrearRepartidorAsync(sucursal.Id, "Rep Historico", "historico");
        var repConActiva = await CrearRepartidorAsync(sucursal.Id, "Rep Con Activa", "activa");

        // repConHistorico tiene 2 entregas: 1 Entregado, 1 NoEntregado (carga activa = 0)
        var ph1 = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        ph1.MarcarListo();
        var ph2 = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        ph2.MarcarListo();
        await _db.SaveChangesAsync();

        var eh1 = new Entrega { PedidoId = ph1.Id, RepartidorId = repConHistorico.Id };
        eh1.MarcarEnCamino();
        eh1.MarcarEntregado();

        var eh2 = new Entrega { PedidoId = ph2.Id, RepartidorId = repConHistorico.Id };
        eh2.MarcarEnCamino();
        eh2.MarcarNoEntregado();

        _db.Entregas.AddRange(eh1, eh2);

        // repConActiva tiene 1 entrega Asignado (carga activa = 1)
        var pa = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        pa.MarcarListo();
        await _db.SaveChangesAsync();
        var ea = new Entrega { PedidoId = pa.Id, RepartidorId = repConActiva.Id };
        _db.Entregas.Add(ea);
        await _db.SaveChangesAsync();

        // Act: nuevo pedido
        var pNuevo = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        var res = await _pedidoService.MarcarListoAsync(pNuevo.Id, sucursal.Id);

        // Assert: repConHistorico tiene carga 0, repConActiva tiene carga 1 -> se asigna a repConHistorico
        Assert.True(res.EsExitoso);
        var eNueva = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == pNuevo.Id);
        Assert.NotNull(eNueva);
        Assert.Equal(repConHistorico.Id, eNueva.RepartidorId);
    }

    [Fact]
    public async Task Test8_NoPermitirDobleEntregaParaElMismoPedido()
    {
        // Arrange
        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var rep1 = await CrearRepartidorAsync(sucursal.Id, "Repartidor 1", "r1");
        var rep2 = await CrearRepartidorAsync(sucursal.Id, "Repartidor 2", "r2");

        var pedido = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);
        pedido.MarcarListo();
        await _db.SaveChangesAsync();

        // 1. Asignación inicial exitosa
        var res1 = await _entregaService.AsignarAutomaticoAsync(pedido.Id);
        Assert.True(res1.EsExitoso);

        // 2. Intento de reasignación cuando ya existe entrega (idempotencia)
        var res2 = await _entregaService.AsignarAutomaticoAsync(pedido.Id);
        Assert.True(res2.EsExitoso);

        // 3. Simulación de concurrencia: un segundo contexto intenta insertar Entrega con el mismo PedidoId
        var options = new DbContextOptionsBuilder<GroceryAppDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var dbConcurrente = new GroceryAppDbContext(options);

        dbConcurrente.Entregas.Add(new Entrega { PedidoId = pedido.Id, RepartidorId = rep2.Id });
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await dbConcurrente.SaveChangesAsync();
        });

        // Assert: solo existe una entrega para este pedido
        var totalEntregas = await _db.Entregas.CountAsync(e => e.PedidoId == pedido.Id);
        Assert.Equal(1, totalEntregas);
    }

    [Fact]
    public async Task Test9_EstrategiaPuedeSerSustituida_MedianteIEstrategiaAsignacion()
    {
        // Arrange: crear una estrategia alternativa personalizada
        var estrategiaAlternativa = new EstrategiaMockSiempreRepartidorEspecifico(999);
        var entregaServiceConMock = new EntregaService(_db, estrategiaAlternativa);
        var pedidoServiceConMock = new PedidoService(_db, entregaServiceConMock);

        var (sucursal, cliente, direccion) = await CrearEntornoBaseAsync();
        var repEspecial = new Empleado
        {
            Id = 999,
            Nombre = "Repartidor Elegido",
            Usuario = "elegido",
            PasswordHash = "hash",
            Rol = RolEmpleado.Repartidor,
            SucursalId = sucursal.Id
        };
        _db.Empleados.Add(repEspecial);
        await _db.SaveChangesAsync();

        var pedido = await CrearPedidoEnPreparacionAsync(sucursal.Id, cliente.Id, direccion.Id);

        // Act
        var res = await pedidoServiceConMock.MarcarListoAsync(pedido.Id, sucursal.Id);

        // Assert: la asignación utilizó la estrategia sustituida
        Assert.True(res.EsExitoso);
        var entrega = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == pedido.Id);
        Assert.NotNull(entrega);
        Assert.Equal(999, entrega.RepartidorId);
    }

    [Fact]
    public async Task AsignacionManual_RepartidorDeOtraSucursal_DebeFallar()
    {
        // Arrange: Sucursal A y Sucursal B
        var (sucursalA, clienteA, dirA) = await CrearEntornoBaseAsync("Sucursal Jinotepe");
        var (sucursalB, _, _) = await CrearEntornoBaseAsync("Sucursal Diriamba");

        // Repartidor en Sucursal B y Repartidor en Sucursal A
        var repB = await CrearRepartidorAsync(sucursalB.Id, "Repartidor Sucursal B", "rep_b");
        var repA = await CrearRepartidorAsync(sucursalA.Id, "Repartidor Sucursal A", "rep_a");

        // Pedido perteneciente a Sucursal A en estado Listo
        var pedidoA = await CrearPedidoEnPreparacionAsync(sucursalA.Id, clienteA.Id, dirA.Id);
        pedidoA.MarcarListo();
        await _db.SaveChangesAsync();

        // Act 1: Intento de asignación manual al repartidor de la Sucursal B (sin scope / vista admin)
        var resSinScope = await _entregaService.AsignarManualAsync(pedidoA.Id, repB.Id, sucursalIdScope: null);

        // Act 1b: Intento de asignación manual al repartidor de la Sucursal B (con scope de Sucursal A / empleado de sucursal)
        var resConScope = await _entregaService.AsignarManualAsync(pedidoA.Id, repB.Id, sucursalIdScope: sucursalA.Id);

        // Assert 1: La asignación a un repartidor de otra sucursal debe fallar y no debe crearse Entrega
        Assert.False(resSinScope.EsExitoso);
        Assert.Contains("sucursal", resSinScope.Error, StringComparison.OrdinalIgnoreCase);

        Assert.False(resConScope.EsExitoso);
        Assert.Contains("sucursal", resConScope.Error, StringComparison.OrdinalIgnoreCase);

        var entregaExiste = await _db.Entregas.AnyAsync(e => e.PedidoId == pedidoA.Id);
        Assert.False(entregaExiste);

        // Act 2 (Caso contrario): Asignación manual al repartidor de la misma Sucursal A
        var resExitoso = await _entregaService.AsignarManualAsync(pedidoA.Id, repA.Id, sucursalIdScope: sucursalA.Id);

        // Assert 2: La asignación dentro de la misma sucursal debe ser exitosa y crear la Entrega
        Assert.True(resExitoso.EsExitoso);

        var entrega = await _db.Entregas.FirstOrDefaultAsync(e => e.PedidoId == pedidoA.Id);
        Assert.NotNull(entrega);
        Assert.Equal(repA.Id, entrega.RepartidorId);
        Assert.Equal(EstadoEntrega.Asignado, entrega.Estado);
    }

    private class EstrategiaMockSiempreRepartidorEspecifico : IEstrategiaAsignacion
    {
        private readonly int _repartidorId;
        public EstrategiaMockSiempreRepartidorEspecifico(int repartidorId) => _repartidorId = repartidorId;

        public Task<int?> SeleccionarRepartidorAsync(int sucursalId, IAppDbContext db, CancellationToken ct = default)
            => Task.FromResult<int?>(_repartidorId);
    }
}

