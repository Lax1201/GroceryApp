class PedidoItemModel {
  final int id;
  final int productoId;
  final String productoNombre;
  final int cantidad;
  final double precioUnitario;
  final double subtotal;

  const PedidoItemModel({
    required this.id,
    required this.productoId,
    required this.productoNombre,
    required this.cantidad,
    required this.precioUnitario,
    required this.subtotal,
  });

  factory PedidoItemModel.fromJson(Map<String, dynamic> json) {
    return PedidoItemModel(
      id: json['id'] as int? ?? 0,
      productoId: json['productoId'] as int? ?? 0,
      productoNombre: json['productoNombre'] as String? ?? '',
      cantidad: json['cantidad'] as int? ?? 0,
      precioUnitario: (json['precioUnitario'] as num?)?.toDouble() ?? 0.0,
      subtotal: (json['subtotal'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class PedidoDetalleModel {
  final int id;
  final String estado;
  final int sucursalId;
  final String direccionReferencia;
  final double tarifaEnvio;
  final double subtotal;
  final double total;
  final DateTime fechaCreacion;
  final List<PedidoItemModel> items;

  const PedidoDetalleModel({
    required this.id,
    required this.estado,
    required this.sucursalId,
    required this.direccionReferencia,
    required this.tarifaEnvio,
    required this.subtotal,
    required this.total,
    required this.fechaCreacion,
    required this.items,
  });

  factory PedidoDetalleModel.fromJson(Map<String, dynamic> json) {
    return PedidoDetalleModel(
      id: json['id'] as int? ?? 0,
      estado: json['estado'] as String? ?? '',
      sucursalId: json['sucursalId'] as int? ?? 0,
      direccionReferencia: json['direccionReferencia'] as String? ?? '',
      tarifaEnvio: (json['tarifaEnvio'] as num?)?.toDouble() ?? 0.0,
      subtotal: (json['subtotal'] as num?)?.toDouble() ?? 0.0,
      total: (json['total'] as num?)?.toDouble() ?? 0.0,
      fechaCreacion: DateTime.tryParse(json['fechaCreacion'] as String? ?? '') ??
          DateTime.now(),
      items: (json['items'] as List<dynamic>? ?? [])
          .map((i) => PedidoItemModel.fromJson(i as Map<String, dynamic>))
          .toList(),
    );
  }
}
