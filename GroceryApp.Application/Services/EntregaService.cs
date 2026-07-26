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

    public EntregaService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>Pedidos "Listo" sin entrega asignada, de la sucursal del repartidor (modelo de pool).</summary>
    public async Task<List<PedidoResumenDto>> ListarDisponiblesAsync(int sucursalIdRepartidor, CancellationToken ct = default)
    {
        return await _db.Pedidos
            .Include(p => p.Cliente)
            .Where(p => p.SucursalId == sucursalIdRepartidor
                        && p.Estado == EstadoPedido.Listo
                        && p.Entrega == null)
            .OrderBy(p => p.FechaCreacion)
            .Select(p => new PedidoResumenDto(p.Id, p.Estado.ToString(), p.Total, p.FechaCreacion, p.Cliente!.Nombre))
            .ToListAsync(ct);
    }

    /// <summary>
    /// El propio repartidor "toma" un pedido disponible. La concurrencia real
    /// (dos repartidores tocando "tomar" al mismo tiempo) la resuelve el índice
    /// único de Entregas.PedidoId (Fase 5) — el chequeo previo es solo para dar
    /// un mensaje amigable en el caso normal, no la protección real.
    /// </summary>
    public async Task<Result> TomarAsync(int pedidoId, int repartidorId, CancellationToken ct = default)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null)
            return Result.Fallido("Pedido no encontrado.");

        if (pedido.Estado != EstadoPedido.Listo)
            return Result.Fallido("Este pedido todavía no está listo para entregar.");

        var yaTomado = await _db.Entregas.AnyAsync(e => e.PedidoId == pedidoId, ct);
        if (yaTomado)
            return Result.Fallido("Este pedido ya fue tomado por otro repartidor.");

        _db.Entregas.Add(new Entrega { PedidoId = pedidoId, RepartidorId = repartidorId });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Result.Fallido("Este pedido ya fue tomado por otro repartidor.");
        }

        return Result.Exitoso();
    }

    /// <summary>Asignación manual de respaldo (empleado de sucursal o admin), no el flujo normal.</summary>
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
            .Where(e => e.RepartidorId == repartidorId
                        && (e.Estado == EstadoEntrega.Asignado || e.Estado == EstadoEntrega.EnCamino))
            .OrderBy(e => e.FechaAsignacion)
            .Select(e => new EntregaDto(
                e.Id, e.PedidoId, e.Estado.ToString(), e.Pedido!.Direccion!.Referencia, e.Pedido.Total, e.FechaAsignacion))
            .ToListAsync(ct);
    }

    public Task<Result> MarcarEnCaminoAsync(int entregaId, int repartidorId, CancellationToken ct = default)
        => EjecutarAsync(entregaId, repartidorId, (e, p) => { e.MarcarEnCamino(); p.MarcarEnCamino(); }, ct);

    public Task<Result> MarcarEntregadoAsync(int entregaId, int repartidorId, CancellationToken ct = default)
        => EjecutarAsync(entregaId, repartidorId, (e, p) => { e.MarcarEntregado(); p.MarcarEntregado(); }, ct);

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
