import 'package:dio/dio.dart';

class AppException implements Exception {
  final String message;
  final int? statusCode;

  const AppException(this.message, [this.statusCode]);

  factory AppException.fromDio(DioException error) {
    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.sendTimeout ||
        error.type == DioExceptionType.receiveTimeout) {
      return const AppException('Tiempo de espera agotado. Verifica tu conexión a internet.');
    }

    if (error.type == DioExceptionType.connectionError) {
      return const AppException('No se pudo conectar al servidor. Revisa tu conexión de red.');
    }

    final response = error.response;
    if (response != null) {
      final statusCode = response.statusCode;
      final data = response.data;

      // RFC 7807 ProblemDetails format
      if (data is Map<String, dynamic>) {
        if (data.containsKey('detail') && data['detail'] != null && data['detail'].toString().isNotEmpty) {
          return AppException(data['detail'].toString(), statusCode);
        }
        if (data.containsKey('title') && data['title'] != null && data['title'].toString().isNotEmpty) {
          return AppException(data['title'].toString(), statusCode);
        }
        if (data.containsKey('error') && data['error'] != null && data['error'].toString().isNotEmpty) {
          return AppException(data['error'].toString(), statusCode);
        }
      }

      if (statusCode == 401) {
        return const AppException('Credenciales incorrectas o sesión expirada.', 401);
      }
      if (statusCode == 403) {
        return const AppException('No tienes permisos para realizar esta acción.', 403);
      }
      if (statusCode == 404) {
        return const AppException('Recurso no encontrado.', 404);
      }
      if (statusCode == 409) {
        return const AppException('Conflicto: el recurso ya existe.', 409);
      }
      if (statusCode == 422) {
        return const AppException('Los datos enviados no son válidos para procesar la solicitud.', 422);
      }
      if (statusCode != null && statusCode >= 500) {
        return AppException('Error interno del servidor. Inténtalo más tarde.', statusCode);
      }
    }

    return AppException(error.message ?? 'Ocurrió un error inesperado.');
  }

  @override
  String toString() => message;
}
