using GroceryApp.Application.Common;
using GroceryApp.Application.Dtos;
using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Application.Services;

public class PedidoService
{
    private readonly IAppDbContext _db;

    public PedidoService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Checkout. Valida: dirección del cliente, catálogo/stock, que todos los
    /// productos sean de la misma sucursal, y horario de atención (Fase 1).
    /// </summary>
    public async Task<Result<PedidoDetalleDto>> CrearAsync(
        int clienteId, int direccionId, List<ItemSolicitado> itemsSolicitados, CancellationToken ct = default)
    {
        if (itemsSolicitados.Count == 0)
            return Result<PedidoDetalleDto>.Fallido("El pedido no puede estar vacío.");

        if (itemsSolicitados.Any(i => i.Cantidad <= 0))
            return Result<PedidoDetalleDto>.Fallido("La cantidad de cada producto debe ser mayor a cero.");

        var direccion = await _db.Direcciones
            .Include(d => d.Zona)
            .FirstOrDefaultAsync(d => d.Id == direccionId && d.ClienteId == clienteId, ct);
        if (direccion is null)
            return Result<PedidoDetalleDto>.Fallido("La dirección indicada no existe o no te pertenece.");

        var productoIds = itemsSolicitados.Select(i => i.ProductoId).Distinct().ToList();
        var productosSucursal = await _db.ProductosSucursal
            .Include(ps => ps.Producto)
            .Where(ps => productoIds.Contains(ps.ProductoId))
            .ToListAsync(ct);

        if (productosSucursal.Count < productoIds.Count)
            return Result<PedidoDetalleDto>.Fallido("Uno o más productos del pedido no existen en el catálogo.");

        var sucursalesDistintas = productosSucursal.Select(ps => ps.SucursalId).Distinct().ToList();
        if (sucursalesDistintas.Count > 1)
            return Result<PedidoDetalleDto>.Fallido("Todos los productos de un pedido deben pertenecer a la misma sucursal.");

        var sucursalId = sucursalesDistintas.Single();
        var sucursal = await _db.Sucursales.FirstOrDefaultAsync(s => s.Id == sucursalId, ct);
        if (sucursal is null)
            return Result<PedidoDetalleDto>.Fallido("La sucursal no existe.");

        // Nicaragua es UTC-6 todo el año (sin horario de verano) — se evita depender
        // de nombres de zona horaria que difieren entre Windows/Linux.
        var horaNicaragua = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        if (!sucursal.EstaAbierta(horaNicaragua))
            return Result<PedidoDetalleDto>.Fallido("La sucursal está fuera de su horario de atención en este momento.");

        var noDisponibles = new List<string>();
        var pedido = new Pedido
        {
            ClienteId = clienteId,
            SucursalId = sucursalId,
            DireccionId = direccionId,
            TarifaEnvio = direccion.Zona!.TarifaEnvio
        };

        foreach (var solicitado in itemsSolicitados)
        {
            var ps = productosSucursal.First(p => p.ProductoId == solicitado.ProductoId);
            if (!ps.StockDisponible)
            {
                noDisponibles.Add(ps.Producto!.Nombre);
                continue;
            }

            pedido.Items.Add(new PedidoItem
            {
                ProductoId = solicitado.ProductoId,
                Cantidad = solicitado.Cantidad,
                PrecioUnitario = ps.Precio
            });
        }

        if (noDisponibles.Count > 0)
            return Result<PedidoDetalleDto>.Fallido(
                $"Estos productos no están disponibles ahora mismo: {string.Join(", ", noDisponibles)}.");

        pedido.RecalcularTotales();

        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync(ct);

        return Result<PedidoDetalleDto>.Exitoso(await MapearDetalleAsync(pedido.Id, ct));
    }

    public async Task<Result<PedidoDetalleDto>> ObtenerAsync(int clienteId, int pedidoId, CancellationToken ct = default)
    {
        var existe = await _db.Pedidos.AnyAsync(p => p.Id == pedidoId && p.ClienteId == clienteId, ct);
        if (!existe)
            return Result<PedidoDetalleDto>.Fallido("Pedido no encontrado.");

        return Result<PedidoDetalleDto>.Exitoso(await MapearDetalleAsync(pedidoId, ct));
    }

    public async Task<List<PedidoResumenDto>> HistorialAsync(int clienteId, CancellationToken ct = default)
    {
        return await _db.Pedidos
            .Where(p => p.ClienteId == clienteId)
            .Include(p => p.Cliente)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new PedidoResumenDto(p.Id, p.Estado.ToString(), p.Total, p.FechaCreacion, p.Cliente!.Nombre))
            .ToListAsync(ct);
    }

    public async Task<Result> CancelarAsync(int clienteId, int pedidoId, CancellationToken ct = default)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId, ct);
        if (pedido is null)
            return Result.Fallido("Pedido no encontrado.");

        return await EjecutarAsync(() => pedido.Cancelar(), ct);
    }

    /// <summary>Panel de sucursal/admin. sucursalIdScope null = Admin (ve todas las sucursales).</summary>
    public async Task<List<PedidoResumenDto>> ListarParaPanelAsync(int? sucursalIdScope, string? estadoFiltro, CancellationToken ct = default)
    {
        var query = _db.Pedidos.Include(p => p.Cliente).AsQueryable();

        if (sucursalIdScope.HasValue)
            query = query.Where(p => p.SucursalId == sucursalIdScope.Value);

        if (!string.IsNullOrWhiteSpace(estadoFiltro) && Enum.TryParse<EstadoPedido>(estadoFiltro, true, out var estadoEnum))
            query = query.Where(p => p.Estado == estadoEnum);

        return await query
            .OrderBy(p => p.FechaCreacion)
            .Select(p => new PedidoResumenDto(p.Id, p.Estado.ToString(), p.Total, p.FechaCreacion, p.Cliente!.Nombre))
            .ToListAsync(ct);
    }

    public Task<Result> ConfirmarAsync(int pedidoId, int? sucursalIdScope, CancellationToken ct = default)
        => CambiarEstadoConScopeAsync(pedidoId, sucursalIdScope, p => p.Confirmar(), ct);

    public Task<Result> RechazarAsync(int pedidoId, int? sucursalIdScope, CancellationToken ct = default)
        => CambiarEstadoConScopeAsync(pedidoId, sucursalIdScope, p => p.Rechazar(), ct);

    public Task<Result> IniciarPreparacionAsync(int pedidoId, int? sucursalIdScope, CancellationToken ct = default)
        => CambiarEstadoConScopeAsync(pedidoId, sucursalIdScope, p => p.IniciarPreparacion(), ct);

    public Task<Result> MarcarListoAsync(int pedidoId, int? sucursalIdScope, CancellationToken ct = default)
        => CambiarEstadoConScopeAsync(pedidoId, sucursalIdScope, p => p.MarcarListo(), ct);

    public async Task<Result> EliminarItemAsync(int pedidoId, int itemId, int? sucursalIdScope, CancellationToken ct = default)
    {
        var pedido = await _db.Pedidos.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null)
            return Result.Fallido("Pedido no encontrado.");

        if (sucursalIdScope.HasValue && pedido.SucursalId != sucursalIdScope.Value)
            return Result.Fallido("Este pedido no pertenece a tu sucursal.");

        return await EjecutarAsync(() => pedido.EliminarItemPorFaltante(itemId), ct);
    }

    private async Task<Result> CambiarEstadoConScopeAsync(
        int pedidoId, int? sucursalIdScope, Action<Pedido> transicion, CancellationToken ct)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null)
            return Result.Fallido("Pedido no encontrado.");

        if (sucursalIdScope.HasValue && pedido.SucursalId != sucursalIdScope.Value)
            return Result.Fallido("Este pedido no pertenece a tu sucursal.");

        return await EjecutarAsync(() => transicion(pedido), ct);
    }

    private async Task<Result> EjecutarAsync(Action transicion, CancellationToken ct)
    {
        try
        {
            transicion();
        }
        catch (DomainException ex)
        {
            return Result.Fallido(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Exitoso();
    }

    private async Task<PedidoDetalleDto> MapearDetalleAsync(int pedidoId, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Items).ThenInclude(i => i.Producto)
            .Include(p => p.Direccion)
            .FirstAsync(p => p.Id == pedidoId, ct);

        return new PedidoDetalleDto(
            pedido.Id,
            pedido.Estado.ToString(),
            pedido.SucursalId,
            pedido.Direccion!.Referencia,
            pedido.TarifaEnvio,
            pedido.Subtotal,
            pedido.Total,
            pedido.FechaCreacion,
            pedido.Items
                .Select(i => new PedidoItemDto(i.Id, i.ProductoId, i.Producto!.Nombre, i.Cantidad, i.PrecioUnitario, i.Subtotal))
                .ToList()
        );
    }
}
