import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/checkout/data/models/pedido_detalle_model.dart';

void main() {
  group('PedidoDetalleModel', () {
    test('parses full JSON correctly', () {
      final json = {
        'id': 42,
        'estado': 'Pendiente',
        'sucursalId': 1,
        'direccionReferencia': 'Frente al parque',
        'tarifaEnvio': 30.0,
        'subtotal': 150.0,
        'total': 180.0,
        'fechaCreacion': '2026-09-25T10:30:00Z',
        'items': [
          {
            'id': 1,
            'productoId': 10,
            'productoNombre': 'Arroz 1lb',
            'cantidad': 2,
            'precioUnitario': 25.0,
            'subtotal': 50.0,
          },
          {
            'id': 2,
            'productoId': 20,
            'productoNombre': 'Frijoles 1lb',
            'cantidad': 4,
            'precioUnitario': 25.0,
            'subtotal': 100.0,
          },
        ],
      };

      final model = PedidoDetalleModel.fromJson(json);

      expect(model.id, 42);
      expect(model.estado, 'Pendiente');
      expect(model.sucursalId, 1);
      expect(model.direccionReferencia, 'Frente al parque');
      expect(model.tarifaEnvio, 30.0);
      expect(model.subtotal, 150.0);
      expect(model.total, 180.0);
      expect(model.items.length, 2);
      expect(model.items[0].productoNombre, 'Arroz 1lb');
      expect(model.items[0].cantidad, 2);
      expect(model.items[1].subtotal, 100.0);
    });

    test('handles missing fields with defaults', () {
      final model = PedidoDetalleModel.fromJson({});

      expect(model.id, 0);
      expect(model.estado, '');
      expect(model.sucursalId, 0);
      expect(model.direccionReferencia, '');
      expect(model.tarifaEnvio, 0.0);
      expect(model.subtotal, 0.0);
      expect(model.total, 0.0);
      expect(model.items, isEmpty);
    });
  });
}
