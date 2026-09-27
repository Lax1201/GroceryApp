import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/pedidos/data/models/pedido_resumen_model.dart';

void main() {
  group('PedidoResumenModel', () {
    test('parses from JSON correctly', () {
      final json = {
        'id': 15,
        'estado': 'EnCamino',
        'total': 250.0,
        'fechaCreacion': '2026-09-25T14:30:00Z',
        'clienteNombre': 'Juan Pérez',
      };

      final model = PedidoResumenModel.fromJson(json);

      expect(model.id, 15);
      expect(model.estado, 'EnCamino');
      expect(model.total, 250.0);
      expect(model.clienteNombre, 'Juan Pérez');
    });

    test('handles missing fields with defaults', () {
      final model = PedidoResumenModel.fromJson({});

      expect(model.id, 0);
      expect(model.estado, '');
      expect(model.total, 0.0);
      expect(model.clienteNombre, '');
    });

    test('estadoLegible translates technical states', () {
      final fecha = DateTime(2026, 9, 25);
      final pendiente = PedidoResumenModel(
        id: 1,
        estado: 'Pendiente',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(pendiente.estadoLegible, 'Pendiente');

      final enPreparacion = PedidoResumenModel(
        id: 1,
        estado: 'EnPreparacion',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(enPreparacion.estadoLegible, 'En preparación');

      final noEntregado = PedidoResumenModel(
        id: 1,
        estado: 'NoEntregado',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(noEntregado.estadoLegible, 'No entregado');
    });

    test('estaActivo returns true for active states', () {
      final fecha = DateTime(2026, 9, 25);
      final activo = PedidoResumenModel(
        id: 1,
        estado: 'EnCamino',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(activo.estaActivo, true);

      final finalizado = PedidoResumenModel(
        id: 1,
        estado: 'Entregado',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(finalizado.estaActivo, false);
    });

    test('puedeCancelarse only for Pendiente and Confirmado', () {
      final fecha = DateTime(2026, 9, 25);
      final pendiente = PedidoResumenModel(
        id: 1,
        estado: 'Pendiente',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(pendiente.puedeCancelarse, true);

      final enPreparacion = PedidoResumenModel(
        id: 1,
        estado: 'EnPreparacion',
        total: 0,
        fechaCreacion: fecha,
        clienteNombre: '',
      );
      expect(enPreparacion.puedeCancelarse, false);
    });
  });
}
