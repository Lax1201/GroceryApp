import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/direcciones/data/models/direccion_model.dart';

void main() {
  group('DireccionModel', () {
    test('parses from JSON correctly', () {
      final json = {
        'id': 5,
        'latitud': 11.8496,
        'longitud': -86.1993,
        'referencia': 'Frente a la farmacia',
        'esPrincipal': true,
        'zonaNombre': 'Casco urbano',
        'tarifaEnvio': 30.0,
      };

      final model = DireccionModel.fromJson(json);

      expect(model.id, 5);
      expect(model.latitud, 11.8496);
      expect(model.longitud, -86.1993);
      expect(model.referencia, 'Frente a la farmacia');
      expect(model.esPrincipal, true);
      expect(model.zonaNombre, 'Casco urbano');
      expect(model.tarifaEnvio, 30.0);
    });

    test('handles missing fields with defaults', () {
      final model = DireccionModel.fromJson({});

      expect(model.id, 0);
      expect(model.latitud, 0.0);
      expect(model.longitud, 0.0);
      expect(model.referencia, '');
      expect(model.esPrincipal, false);
      expect(model.zonaNombre, '');
      expect(model.tarifaEnvio, 0.0);
    });

    test('serializes to JSON correctly', () {
      const model = DireccionModel(
        id: 1,
        latitud: 11.5,
        longitud: -86.5,
        referencia: 'Casa azul',
        esPrincipal: false,
        zonaNombre: 'Casco urbano',
        tarifaEnvio: 25.0,
      );

      final json = model.toJson();

      expect(json['id'], 1);
      expect(json['latitud'], 11.5);
      expect(json['longitud'], -86.5);
      expect(json['referencia'], 'Casa azul');
      expect(json['esPrincipal'], false);
      expect(json['zonaNombre'], 'Casco urbano');
      expect(json['tarifaEnvio'], 25.0);
    });
  });
}
