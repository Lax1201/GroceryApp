import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_client.dart';
import 'models/direccion_model.dart';

class DireccionesRepository {
  final Dio _dio;

  DireccionesRepository(this._dio);

  Future<List<DireccionModel>> listar() async {
    try {
      final response = await _dio.get('/direcciones');
      final list = response.data as List<dynamic>;
      return list
          .map((item) => DireccionModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<DireccionModel> crear({
    required double latitud,
    required double longitud,
    required String referencia,
    required bool esPrincipal,
  }) async {
    try {
      final response = await _dio.post(
        '/direcciones',
        data: {
          'latitud': latitud,
          'longitud': longitud,
          'referencia': referencia.trim(),
          'esPrincipal': esPrincipal,
        },
      );
      return DireccionModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<DireccionModel> actualizar({
    required int id,
    required double latitud,
    required double longitud,
    required String referencia,
    required bool esPrincipal,
  }) async {
    try {
      final response = await _dio.put(
        '/direcciones/$id',
        data: {
          'latitud': latitud,
          'longitud': longitud,
          'referencia': referencia.trim(),
          'esPrincipal': esPrincipal,
        },
      );
      return DireccionModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<void> eliminar(int id) async {
    try {
      await _dio.delete('/direcciones/$id');
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }
}

final direccionesRepositoryProvider = Provider<DireccionesRepository>((ref) {
  final dio = ref.watch(dioProvider);
  return DireccionesRepository(dio);
});
