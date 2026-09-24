import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/auth/data/models/auth_response_model.dart';
import 'package:grocery_app_mobile/features/catalog/data/models/category_model.dart';
import 'package:grocery_app_mobile/features/catalog/data/models/product_model.dart';

void main() {
  group('Model Parsing & Serialization Tests', () {
    test('CategoriaModel parses from JSON and serializes to JSON correctly', () {
      final json = {
        'id': 3,
        'nombre': 'Lácteos y huevos',
      };

      final model = CategoriaModel.fromJson(json);

      expect(model.id, 3);
      expect(model.nombre, 'Lácteos y huevos');

      final output = model.toJson();
      expect(output['id'], 3);
      expect(output['nombre'], 'Lácteos y huevos');
    });

    test('ProductoModel parses from JSON with all fields correctly', () {
      final json = {
        'id': 10,
        'nombre': 'Arroz Blanco 1lb',
        'descripcion': 'Grano entero seleccionado',
        'categoriaId': 1,
        'categoriaNombre': 'Granos básicos',
        'fotoUrl': '/uploads/productos/arroz.jpg',
        'precio': 25.50,
        'stockDisponible': true,
      };

      final model = ProductoModel.fromJson(json);

      expect(model.id, 10);
      expect(model.nombre, 'Arroz Blanco 1lb');
      expect(model.descripcion, 'Grano entero seleccionado');
      expect(model.categoriaId, 1);
      expect(model.categoriaNombre, 'Granos básicos');
      expect(model.fotoUrl, '/uploads/productos/arroz.jpg');
      expect(model.precio, 25.50);
      expect(model.stockDisponible, isTrue);

      final output = model.toJson();
      expect(output['id'], 10);
      expect(output['precio'], 25.50);
      expect(output['stockDisponible'], isTrue);
    });

    test('ProductoModel handles nullables and default values', () {
      final json = {
        'id': 12,
        'nombre': 'Frijoles Rojos',
        'categoriaId': 1,
        'categoriaNombre': 'Granos',
        'precio': 30,
      };

      final model = ProductoModel.fromJson(json);

      expect(model.id, 12);
      expect(model.descripcion, isNull);
      expect(model.fotoUrl, isNull);
      expect(model.precio, 30.0);
      expect(model.stockDisponible, isFalse);
    });

    test('AuthResponseModel parses token from JSON correctly', () {
      const tokenString = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
      final json = {'token': tokenString};

      final model = AuthResponseModel.fromJson(json);

      expect(model.token, tokenString);
      expect(model.toJson()['token'], tokenString);
    });
  });
}
