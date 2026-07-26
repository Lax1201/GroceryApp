using GroceryApp.Domain.Enums;
using GroceryApp.Domain.Exceptions;

namespace GroceryApp.Domain.Entities;

public class Pedido
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public int DireccionId { get; set; }
    public Direccion? Direccion { get; set; }

    public EstadoPedido Estado { get; private set; } = EstadoPedido.Pendiente;

    public decimal TarifaEnvio { get; set; }
    public decimal Subtotal { get; private set; }
    public decimal Total { get; private set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; private set; } = DateTime.UtcNow;

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
    public Entrega? Entrega { get; set; }

    /// <summary>
    /// Recalcula Subtotal/Total a partir de los Items actuales.
    /// EF Core necesita poder asignar Estado/Total al materializar desde la BD,
    /// así que esto se llama explícitamente después de tocar Items, no en cada get.
    /// </summary>
    public void RecalcularTotales()
    {
        Subtotal = Items.Sum(i => i.Subtotal);
        Total = Subtotal + TarifaEnvio;
        FechaActualizacion = DateTime.UtcNow;
    }

    // --- Transiciones de estado (Fase 1) — cada una valida el estado de origen ---
    // Pendiente → Confirmado → EnPreparacion → Listo → EnCamino → Entregado
    //     ↓                                                  ↓
    // Rechazado/Cancelado                                NoEntregado

    public void Confirmar()
    {
        if (Estado != EstadoPedido.Pendiente)
            throw new DomainException($"No se puede confirmar un pedido en estado {Estado}.");
        Estado = EstadoPedido.Confirmado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Rechazar()
    {
        if (Estado != EstadoPedido.Pendiente)
            throw new DomainException($"No se puede rechazar un pedido en estado {Estado}.");
        Estado = EstadoPedido.Rechazado;
        FechaActualizacion = DateTime.UtcNow;
    }

    /// <summary>Regla de Fase 1: el cliente solo puede cancelar antes de que empiece la preparación.</summary>
    public void Cancelar()
    {
        if (Estado != EstadoPedido.Pendiente)
            throw new DomainException("Solo se puede cancelar un pedido antes de que empiece su preparación.");
        Estado = EstadoPedido.Cancelado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void IniciarPreparacion()
    {
        if (Estado != EstadoPedido.Confirmado)
            throw new DomainException($"No se puede iniciar la preparación de un pedido en estado {Estado}.");
        Estado = EstadoPedido.EnPreparacion;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarListo()
    {
        if (Estado != EstadoPedido.EnPreparacion)
            throw new DomainException($"No se puede marcar listo un pedido en estado {Estado}.");
        Estado = EstadoPedido.Listo;
        FechaActualizacion = DateTime.UtcNow;
    }

    /// <summary>Regla de Fase 1: producto no disponible al preparar -> se elimina y se recalcula el total.</summary>
    public void EliminarItemPorFaltante(int pedidoItemId)
    {
        if (Estado != EstadoPedido.EnPreparacion)
            throw new DomainException("Solo se pueden quitar productos mientras el pedido está en preparación.");

        var item = Items.FirstOrDefault(i => i.Id == pedidoItemId);
        if (item is null)
            throw new DomainException("El producto indicado no pertenece a este pedido.");

        Items.Remove(item);
        RecalcularTotales();
    }

    public void MarcarEnCamino()
    {
        if (Estado != EstadoPedido.Listo)
            throw new DomainException($"No se puede marcar en camino un pedido en estado {Estado}.");
        Estado = EstadoPedido.EnCamino;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarEntregado()
    {
        if (Estado != EstadoPedido.EnCamino)
            throw new DomainException($"No se puede marcar entregado un pedido en estado {Estado}.");
        Estado = EstadoPedido.Entregado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarNoEntregado()
    {
        if (Estado != EstadoPedido.EnCamino)
            throw new DomainException($"No se puede marcar no-entregado un pedido en estado {Estado}.");
        Estado = EstadoPedido.NoEntregado;
        FechaActualizacion = DateTime.UtcNow;
    }
}
