import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../catalog/data/models/product_model.dart';
import '../../domain/cart_item.dart';

class CartState {
  final Map<int, CartItem> items;

  const CartState({this.items = const {}});

  int get totalItemCount => items.values.fold(0, (sum, item) => sum + item.cantidad);

  double get subtotal => items.values.fold(0.0, (sum, item) => sum + item.subtotal);

  bool get isEmpty => items.isEmpty;

  bool get isNotEmpty => items.isNotEmpty;

  CartState copyWith({
    Map<int, CartItem>? items,
  }) {
    return CartState(
      items: items ?? this.items,
    );
  }
}

class CartNotifier extends Notifier<CartState> {
  @override
  CartState build() => const CartState();

  void agregarProducto(ProductoModel producto, [int cantidad = 1]) {
    if (!producto.stockDisponible || cantidad <= 0) return;

    final currentItems = Map<int, CartItem>.from(state.items);
    if (currentItems.containsKey(producto.id)) {
      final existing = currentItems[producto.id]!;
      currentItems[producto.id] = existing.copyWith(
        cantidad: existing.cantidad + cantidad,
      );
    } else {
      currentItems[producto.id] = CartItem(
        producto: producto,
        cantidad: cantidad,
      );
    }
    state = state.copyWith(items: currentItems);
  }

  void incrementarCantidad(int productoId) {
    if (!state.items.containsKey(productoId)) return;
    final currentItems = Map<int, CartItem>.from(state.items);
    final existing = currentItems[productoId]!;
    currentItems[productoId] = existing.copyWith(
      cantidad: existing.cantidad + 1,
    );
    state = state.copyWith(items: currentItems);
  }

  void decrementarCantidad(int productoId) {
    if (!state.items.containsKey(productoId)) return;
    final currentItems = Map<int, CartItem>.from(state.items);
    final existing = currentItems[productoId]!;
    if (existing.cantidad > 1) {
      currentItems[productoId] = existing.copyWith(
        cantidad: existing.cantidad - 1,
      );
    } else {
      currentItems.remove(productoId);
    }
    state = state.copyWith(items: currentItems);
  }

  void eliminarProducto(int productoId) {
    if (!state.items.containsKey(productoId)) return;
    final currentItems = Map<int, CartItem>.from(state.items);
    currentItems.remove(productoId);
    state = state.copyWith(items: currentItems);
  }

  void vaciarCarrito() {
    state = const CartState();
  }
}

final cartProvider = NotifierProvider<CartNotifier, CartState>(CartNotifier.new);
