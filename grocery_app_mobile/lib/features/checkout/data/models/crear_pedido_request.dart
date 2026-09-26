class ItemPedidoRequest {
  final int productoId;
  final int cantidad;

  const ItemPedidoRequest({
    required this.productoId,
    required this.cantidad,
  });

  Map<String, dynamic> toJson() => {
        'productoId': productoId,
        'cantidad': cantidad,
      };
}

class CrearPedidoRequest {
  final int direccionId;
  final List<ItemPedidoRequest> items;

  const CrearPedidoRequest({
    required this.direccionId,
    required this.items,
  });

  Map<String, dynamic> toJson() => {
        'direccionId': direccionId,
        'items': items.map((i) => i.toJson()).toList(),
      };
}
