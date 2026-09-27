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
      const pendiente = PedidoResumenModel(
        id: 1,
        estado: 'Pendiente',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(pendiente.estadoLegible, 'Pendiente');

      const enPreparacion = PedidoResumenModel(
        id: 1,
        estado: 'EnPreparacion',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(enPreparacion.estadoLegible, 'En preparación');

      const noEntregado = PedidoResumenModel(
        id: 1,
        estado: 'NoEntregado',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(noEntregado.estadoLegible, 'No entregado');
    });

    test('estaActivo returns true for active states', () {
      const activo = PedidoResumenModel(
        id: 1,
        estado: 'EnCamino',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(activo.estaActivo, true);

      const finalizado = PedidoResumenModel(
        id: 1,
        estado: 'Entregado',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(finalizado.estaActivo, false);
    });

    test('puedeCancelarse only for Pendiente and Confirmado', () {
      const pendiente = PedidoResumenModel(
        id: 1,
        estado: 'Pendiente',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(pendiente.puedeCancelarse, true);

      const enPreparacion = PedidoResumenModel(
        id: 1,
        estado: 'EnPreparacion',
        total: 0,
        fechaCreacion: null,
        clienteNombre: '',
      );
      expect(enPreparacion.puedeCancelarse, false);
    });
  });
}
