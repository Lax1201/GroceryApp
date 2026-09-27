import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/pedido_resumen_model.dart';
import '../../data/pedidos_repository.dart';
import '../../../checkout/data/models/pedido_detalle_model.dart';

/// Historial de pedidos del cliente autenticado.
final historialProvider = FutureProvider<List<PedidoResumenModel>>((ref) async {
  final repository = ref.watch(pedidosRepositoryProvider);
  return await repository.historial();
});

/// Seguimiento en vivo de un pedido específico (se refresca por polling).
final seguimientoProvider =
    FutureProvider.family<PedidoDetalleModel, int>((ref, pedidoId) async {
  final repository = ref.watch(pedidosRepositoryProvider);
  return await repository.seguimiento(pedidoId);
});

/// Detalle de un pedido pasado (mismo endpoint que seguimiento, sin polling).
final pedidoDetalleProvider =
    FutureProvider.family<PedidoDetalleModel, int>((ref, pedidoId) async {
  final repository = ref.watch(pedidosRepositoryProvider);
  return await repository.obtener(pedidoId);
});
