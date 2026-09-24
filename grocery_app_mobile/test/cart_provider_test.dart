import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/cart/presentation/providers/cart_provider.dart';
import 'package:grocery_app_mobile/features/catalog/data/models/product_model.dart';

void main() {
  group('CartNotifier and CartState Unit Tests', () {
    late ProviderContainer container;

    const productoDisponible1 = ProductoModel(
      id: 1,
      nombre: 'Arroz 1lb',
      categoriaId: 1,
      categoriaNombre: 'Granos',
      precio: 20.0,
      stockDisponible: true,
    );

    const productoDisponible2 = ProductoModel(
      id: 2,
      nombre: 'Aceite 1L',
      categoriaId: 1,
      categoriaNombre: 'Granos',
      precio: 60.0,
      stockDisponible: true,
    );

    const productoAgotado = ProductoModel(
      id: 3,
      nombre: 'Café 400g',
      categoriaId: 2,
      categoriaNombre: 'Bebidas',
      precio: 95.0,
      stockDisponible: false,
    );

    setUp(() {
      container = ProviderContainer();
    });

    tearDown(() {
      container.dispose();
    });

    test('Initial cart state is empty with 0 items and 0 subtotal', () {
      final state = container.read(cartProvider);
      expect(state.isEmpty, isTrue);
      expect(state.totalItemCount, 0);
      expect(state.subtotal, 0.0);
    });

    test('Add available product creates a new cart item', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoDisponible1, 2);

      final state = container.read(cartProvider);
      expect(state.isNotEmpty, isTrue);
      expect(state.totalItemCount, 2);
      expect(state.subtotal, 40.0);
      expect(state.items.containsKey(1), isTrue);
      expect(state.items[1]!.cantidad, 2);
      expect(state.items[1]!.subtotal, 40.0);
    });

    test('Add out-of-stock product does not add anything to cart', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoAgotado, 1);

      final state = container.read(cartProvider);
      expect(state.isEmpty, isTrue);
      expect(state.totalItemCount, 0);
      expect(state.subtotal, 0.0);
    });

    test('Add same product again increments its quantity', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoDisponible1, 2);
      notifier.agregarProducto(productoDisponible1, 3);

      final state = container.read(cartProvider);
      expect(state.totalItemCount, 5);
      expect(state.items[1]!.cantidad, 5);
      expect(state.subtotal, 100.0);
    });

    test('Increment and decrement quantity works as expected', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoDisponible1, 2);

      notifier.incrementarCantidad(1);
      expect(container.read(cartProvider).items[1]!.cantidad, 3);
      expect(container.read(cartProvider).subtotal, 60.0);

      notifier.decrementarCantidad(1);
      expect(container.read(cartProvider).items[1]!.cantidad, 2);
      expect(container.read(cartProvider).subtotal, 40.0);
    });

    test('Decrement quantity to 0 removes product from cart', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoDisponible1, 1);

      notifier.decrementarCantidad(1);
      final state = container.read(cartProvider);
      expect(state.isEmpty, isTrue);
      expect(state.totalItemCount, 0);
      expect(state.subtotal, 0.0);
    });

    test('Eliminar producto removes specific product directly', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoDisponible1, 2);
      notifier.agregarProducto(productoDisponible2, 1);

      expect(container.read(cartProvider).totalItemCount, 3);
      expect(container.read(cartProvider).subtotal, 100.0);

      notifier.eliminarProducto(1);
      final state = container.read(cartProvider);
      expect(state.items.containsKey(1), isFalse);
      expect(state.items.containsKey(2), isTrue);
      expect(state.totalItemCount, 1);
      expect(state.subtotal, 60.0);
    });

    test('Vaciar carrito removes all products', () {
      final notifier = container.read(cartProvider.notifier);
      notifier.agregarProducto(productoDisponible1, 2);
      notifier.agregarProducto(productoDisponible2, 3);

      notifier.vaciarCarrito();
      final state = container.read(cartProvider);
      expect(state.isEmpty, isTrue);
      expect(state.totalItemCount, 0);
      expect(state.subtotal, 0.0);
    });
  });
}
