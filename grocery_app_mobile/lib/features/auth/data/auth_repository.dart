import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_client.dart';
import 'models/auth_response_model.dart';

class AuthRepository {
  final Dio _dio;

  AuthRepository(this._dio);

  Future<AuthResponseModel> login({
    required String telefono,
    required String password,
  }) async {
    try {
      final response = await _dio.post(
        '/auth/cliente/login',
        data: {
          'telefono': telefono.trim(),
          'password': password,
        },
      );
      return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<AuthResponseModel> register({
    required String nombre,
    required String telefono,
    required String password,
    String? email,
  }) async {
    try {
      final response = await _dio.post(
        '/auth/cliente/registro',
        data: {
          'nombre': nombre.trim(),
          'telefono': telefono.trim(),
          'password': password,
          if (email != null && email.trim().isNotEmpty) 'email': email.trim(),
        },
      );
      return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }
}

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  final dio = ref.watch(dioProvider);
  return AuthRepository(dio);
});
