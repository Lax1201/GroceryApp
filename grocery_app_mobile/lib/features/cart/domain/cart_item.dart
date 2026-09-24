import '../../catalog/data/models/product_model.dart';

class CartItem {
  final ProductoModel producto;
  final int cantidad;

  const CartItem({
    required this.producto,
    required this.cantidad,
  });

  double get subtotal => producto.precio * cantidad;

  CartItem copyWith({
    ProductoModel? producto,
    int? cantidad,
  }) {
    return CartItem(
      producto: producto ?? this.producto,
      cantidad: cantidad ?? this.cantidad,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is CartItem &&
          runtimeType == other.runtimeType &&
          producto.id == other.producto.id &&
          cantidad == other.cantidad;

  @override
  int get hashCode => producto.id.hashCode ^ cantidad.hashCode;
}
