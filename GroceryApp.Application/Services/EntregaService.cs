using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class EntregaService
{
    private readonly IAppDbContext _db;
    private readonly IEstrategiaAsignacion _estrategiaAsignacion;

    public EntregaService(IAppDbContext db, IEstrategiaAsignacion estrategiaAsignacion)
    {
        _db = db;
        _estrategiaAsignacion = estrategiaAsignacion;
    }

    /// <summary>
    /// Intenta asignar automáticamente un repartidor elegible a un pedido en estado Listo.
    /// Si no hay repartidores disponibles en la sucursal, el pedido permanece en Listo dentro del pool.
    /// </summary>
    public async Task<Result> AsignarAutomaticoAsync(int pedidoId, CancellationToken ct = default)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null)
            return Result.Fallido("Pedido no encontrado.");

        if (pedido.Estado != EstadoPedido.Listo)
            return Result.Fallido("Solo se pueden asignar pedidos en estado Listo.");

        var yaAsignado = await _db.Entregas.AnyAsync(e => e.PedidoId == pedidoId, ct);
        if (yaAsignado)
            return Result.Exitoso(); // Ya cuenta con asignación activa

        var repartidorId = await _estrategiaAsignacion.SeleccionarRepartidorAsync(pedido.SucursalId, _db, ct);
        if (!repartidorId.HasValue)
        {
            // No hay repartidores disponibles; permanece en Listo dentro del Pool de pendientes
            return Result.Exitoso();
        }

        _db.Entregas.Add(new Entrega { PedidoId = pedidoId, RepartidorId = repartidorId.Value });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Protección de concurrencia contra asignación simultánea
            return Result.Fallido("Este pedido ya fue asignado concurrentemente.");
        }

        return Result.Exitoso();
    }

    /// <summary>
    /// Procesa los pedidos que quedaron pendientes en el pool de una sucursal e intenta
    /// asignarlos a los repartidores disponibles.
    /// </summary>
    public async Task ProcesarPendientesPoolAsync(int sucursalId, CancellationToken ct = default)
    {
        var pedidosPendientes = await _db.Pedidos
            .Where(p => p.SucursalId == sucursalId && p.Estado == EstadoPedido.Listo && p.Entrega == null)
            .OrderBy(p => p.FechaCreacion)
            .ToListAsync(ct);

        foreach (var pedido in pedidosPendientes)
        {
            var repartidorDisponible = await _estrategiaAsignacion.SeleccionarRepartidorAsync(sucursalId, _db, ct);
            if (!repartidorDisponible.HasValue)
                break;

            var res = await AsignarAutomaticoAsync(pedido.Id, ct);
            if (!res.EsExitoso)
                break;
        }
    }

    /// <summary>Pedidos "Listo" sin entrega asignada de la sucursal (para supervisión de pool).</summary>
    public async Task<List<PedidoPoolDto>> ListarPoolDisponiblesAsync(int sucursalId, CancellationToken ct = default)
    {
        return await _db.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Direccion)
            .Where(p => p.SucursalId == sucursalId
                        && p.Estado == EstadoPedido.Listo
                        && p.Entrega == null)
            .OrderBy(p => p.FechaCreacion)
            .Select(p => new PedidoPoolDto(
                p.Id,
                p.Estado.ToString(),
                p.Total,
                p.FechaCreacion,
                p.Cliente != null ? p.Cliente.Nombre : "Cliente",
                p.Direccion != null ? p.Direccion.Referencia : "",
                p.SucursalId
            ))
            .ToListAsync(ct);
    }

    /// <summary>Lista los repartidores activos de una sucursal para la asignación manual de respaldo.</summary>
    public async Task<List<RepartidorDto>> ListarRepartidoresPorSucursalAsync(int? sucursalId, CancellationToken ct = default)
    {
        var query = _db.Empleados
            .Where(e => e.Rol == RolEmpleado.Repartidor)
            .AsQueryable();

        if (sucursalId.HasValue)
            query = query.Where(e => e.SucursalId == sucursalId.Value);

        return await query
            .OrderBy(e => e.Nombre)
            .Select(e => new RepartidorDto(e.Id, e.Nombre, e.Usuario, e.SucursalId))
            .ToListAsync(ct);
    }

    /// <summary>Asignación manual de respaldo (empleado de sucursal o admin).</summary>
    public async Task<Result> AsignarManualAsync(int pedidoId, int repartidorId, int? sucursalIdScope, CancellationToken ct = default)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null)
            return Result.Fallido("Pedido no encontrado.");

        if (sucursalIdScope.HasValue && pedido.SucursalId != sucursalIdScope.Value)
            return Result.Fallido("Este pedido no pertenece a tu sucursal.");

        if (pedido.Estado != EstadoPedido.Listo)
            return Result.Fallido("Solo se puede asignar un repartidor a un pedido en estado Listo.");

        var repartidor = await _db.Empleados
            .FirstOrDefaultAsync(e => e.Id == repartidorId && e.Rol == RolEmpleado.Repartidor, ct);
        if (repartidor is null)
            return Result.Fallido("El repartidor indicado no existe.");

        var yaAsignado = await _db.Entregas.AnyAsync(e => e.PedidoId == pedidoId, ct);
        if (yaAsignado)
            return Result.Fallido("Este pedido ya tiene un repartidor asignado.");

        _db.Entregas.Add(new Entrega { PedidoId = pedidoId, RepartidorId = repartidorId });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Result.Fallido("Este pedido ya tiene un repartidor asignado.");
        }

        return Result.Exitoso();
    }

    public async Task<List<EntregaDto>> MisEntregasAsync(int repartidorId, CancellationToken ct = default)
    {
        return await _db.Entregas
            .Include(e => e.Pedido).ThenInclude(p => p!.Direccion)
            .Include(e => e.Pedido).ThenInclude(p => p!.Cliente)
            .Where(e => e.RepartidorId == repartidorId
                        && (e.Estado == EstadoEntrega.Asignado || e.Estado == EstadoEntrega.EnCamino))
            .OrderBy(e => e.FechaAsignacion)
            .Select(e => new EntregaDto(
                e.Id,
                e.PedidoId,
                e.Estado.ToString(),
                e.Pedido!.Direccion != null ? e.Pedido.Direccion.Referencia : "",
                e.Pedido.Total,
                e.FechaAsignacion,
                e.Pedido.Cliente != null ? e.Pedido.Cliente.Nombre : "Cliente",
                e.Pedido.Cliente != null ? e.Pedido.Cliente.Telefono : ""))
            .ToListAsync(ct);
    }

    public Task<Result> MarcarEnCaminoAsync(int entregaId, int repartidorId, CancellationToken ct = default)
        => EjecutarAsync(entregaId, repartidorId, (e, p) => { e.MarcarEnCamino(); p.MarcarEnCamino(); }, ct);

    public async Task<Result> MarcarEntregadoAsync(int entregaId, int repartidorId, CancellationToken ct = default)
    {
        var entrega = await _db.Entregas.Include(e => e.Pedido).FirstOrDefaultAsync(e => e.Id == entregaId, ct);
        if (entrega is null)
            return Result.Fallido("Entrega no encontrada.");

        if (entrega.RepartidorId != repartidorId)
            return Result.Fallido("Esta entrega no está asignada a vos.");

        try
        {
            entrega.MarcarEntregado();
            entrega.Pedido!.MarcarEntregado();
        }
        catch (DomainException ex)
        {
            return Result.Fallido(ex.Message);
        }

        await _db.SaveChangesAsync(ct);

        // Al liberarse el repartidor, procesar pedidos pendientes que aguardan en el Pool de la sucursal
        await ProcesarPendientesPoolAsync(entrega.Pedido.SucursalId, ct);

        return Result.Exitoso();
    }

    /// <summary>Regla de Fase 1: no-show incrementa el contador del cliente para revisión posterior.</summary>
    public async Task<Result> MarcarNoEntregadoAsync(int entregaId, int repartidorId, CancellationToken ct = default)
    {
        var entrega = await _db.Entregas
            .Include(e => e.Pedido).ThenInclude(p => p!.Cliente)
            .FirstOrDefaultAsync(e => e.Id == entregaId, ct);

        if (entrega is null)
            return Result.Fallido("Entrega no encontrada.");

        if (entrega.RepartidorId != repartidorId)
            return Result.Fallido("Esta entrega no está asignada a vos.");

        try
        {
            entrega.MarcarNoEntregado();
            entrega.Pedido!.MarcarNoEntregado();
        }
        catch (DomainException ex)
        {
            return Result.Fallido(ex.Message);
        }

        entrega.Pedido.Cliente!.NoShowCount++;

        await _db.SaveChangesAsync(ct);

        // Al finalizar la entrega, procesar pedidos pendientes en el Pool de la sucursal
        await ProcesarPendientesPoolAsync(entrega.Pedido.SucursalId, ct);

        return Result.Exitoso();
    }

    private async Task<Result> EjecutarAsync(
        int entregaId, int repartidorId, Action<Entrega, Pedido> transicion, CancellationToken ct)
    {
        var entrega = await _db.Entregas.Include(e => e.Pedido).FirstOrDefaultAsync(e => e.Id == entregaId, ct);
        if (entrega is null)
            return Result.Fallido("Entrega no encontrada.");

        if (entrega.RepartidorId != repartidorId)
            return Result.Fallido("Esta entrega no está asignada a vos.");

        try
        {
            transicion(entrega, entrega.Pedido!);
        }
        catch (DomainException ex)
        {
            return Result.Fallido(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Exitoso();
    }
}
