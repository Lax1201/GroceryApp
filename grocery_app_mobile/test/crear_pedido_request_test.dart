import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/checkout/data/models/crear_pedido_request.dart';

void main() {
  group('CrearPedidoRequest', () {
    test('serializes to JSON with correct field names', () {
      const request = CrearPedidoRequest(
        direccionId: 7,
        items: [
          ItemPedidoRequest(productoId: 1, cantidad: 2),
          ItemPedidoRequest(productoId: 5, cantidad: 1),
        ],
      );

      final json = request.toJson();

      expect(json['direccionId'], 7);
      expect(json['items'], isA<List<dynamic>>());
      expect((json['items'] as List).length, 2);
      expect((json['items'] as List)[0]['productoId'], 1);
      expect((json['items'] as List)[0]['cantidad'], 2);
      expect((json['items'] as List)[1]['productoId'], 5);
      expect((json['items'] as List)[1]['cantidad'], 1);
    });

    test('handles empty items list', () {
      const request = CrearPedidoRequest(direccionId: 1, items: []);
      final json = request.toJson();

      expect(json['direccionId'], 1);
      expect((json['items'] as List).isEmpty, true);
    });
  });
}
