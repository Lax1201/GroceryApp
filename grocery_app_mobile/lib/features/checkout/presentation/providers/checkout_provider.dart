import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../cart/presentation/providers/cart_provider.dart';
import '../../../direcciones/data/models/direccion_model.dart';
import '../../../direcciones/presentation/providers/direcciones_provider.dart';
import '../../data/models/crear_pedido_request.dart';
import '../../data/models/pedido_detalle_model.dart';
import '../../data/pedidos_repository.dart';

class CheckoutState {
  final bool enviando;
  final String? errorMessage;
  final PedidoDetalleModel? pedidoCreado;

  const CheckoutState({
    this.enviando = false,
    this.errorMessage,
    this.pedidoCreado,
  });

  CheckoutState copyWith({
    bool? enviando,
    String? errorMessage,
    PedidoDetalleModel? pedidoCreado,
    bool clearError = false,
    bool clearPedido = false,
  }) {
    return CheckoutState(
      enviando: enviando ?? this.enviando,
      errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
      pedidoCreado: clearPedido ? null : (pedidoCreado ?? this.pedidoCreado),
    );
  }
}

class CheckoutNotifier extends Notifier<CheckoutState> {
  @override
  CheckoutState build() => const CheckoutState();

  /// Confirma el pedido. Devuelve el pedido creado o null si falló.
  Future<PedidoDetalleModel?> confirmar({
    required DireccionModel direccion,
  }) async {
    final cartState = ref.read(cartProvider);
    if (cartState.isEmpty) {
      state = state.copyWith(
        errorMessage: 'El carrito está vacío.',
        clearPedido: true,
      );
      return null;
    }

    state = state.copyWith(
      enviando: true,
      clearError: true,
      clearPedido: true,
    );

    try {
      final items = cartState.items.values
          .map((item) => ItemPedidoRequest(
                productoId: item.producto.id,
                cantidad: item.cantidad,
              ))
          .toList();

      final request = CrearPedidoRequest(
        direccionId: direccion.id,
        items: items,
      );

      final pedido =
          await ref.read(pedidosRepositoryProvider).crear(request);

      // Vaciar carrito tras éxito
      ref.read(cartProvider.notifier).vaciarCarrito();
      ref.read(direccionSeleccionadaProvider.notifier).seleccionar(null);

      state = state.copyWith(
        enviando: false,
        pedidoCreado: pedido,
        clearError: true,
      );
      return pedido;
    } catch (e) {
      state = state.copyWith(
        enviando: false,
        errorMessage: e.toString(),
      );
      return null;
    }
  }

  void limpiarError() {
    state = state.copyWith(clearError: true);
  }
}

final checkoutProvider =
    NotifierProvider<CheckoutNotifier, CheckoutState>(CheckoutNotifier.new);
