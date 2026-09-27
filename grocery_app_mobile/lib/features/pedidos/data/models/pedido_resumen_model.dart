class PedidoResumenModel {
  final int id;
  final String estado;
  final double total;
  final DateTime fechaCreacion;
  final String clienteNombre;

  const PedidoResumenModel({
    required this.id,
    required this.estado,
    required this.total,
    required this.fechaCreacion,
    required this.clienteNombre,
  });

  factory PedidoResumenModel.fromJson(Map<String, dynamic> json) {
    return PedidoResumenModel(
      id: json['id'] as int? ?? 0,
      estado: json['estado'] as String? ?? '',
      total: (json['total'] as num?)?.toDouble() ?? 0.0,
      fechaCreacion: DateTime.tryParse(json['fechaCreacion'] as String? ?? '') ??
          DateTime.now(),
      clienteNombre: json['clienteNombre'] as String? ?? '',
    );
  }

  /// Etiqueta legible para el cliente (Fase 2: "No entregado" en vez de "NoEntregado").
  String get estadoLegible {
    switch (estado) {
      case 'Pendiente':
        return 'Pendiente';
      case 'Confirmado':
        return 'Confirmado';
      case 'EnPreparacion':
        return 'En preparación';
      case 'Listo':
        return 'Listo para entrega';
      case 'EnCamino':
        return 'En camino';
      case 'Entregado':
        return 'Entregado';
      case 'NoEntregado':
        return 'No entregado';
      case 'Cancelado':
        return 'Cancelado';
      case 'Rechazado':
        return 'Rechazado';
      default:
        return estado;
    }
  }

  /// Indica si el pedido sigue activo (no finalizado).
  bool get estaActivo {
    return estado != 'Entregado' &&
        estado != 'NoEntregado' &&
        estado != 'Cancelado' &&
        estado != 'Rechazado';
  }

  /// Indica si el pedido puede cancelarse (solo antes de EnPreparacion).
  bool get puedeCancelarse {
    return estado == 'Pendiente' || estado == 'Confirmado';
  }
}
