import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_client.dart';
import '../../checkout/data/models/crear_pedido_request.dart';
import '../../checkout/data/models/pedido_detalle_model.dart';
import 'models/pedido_resumen_model.dart';

class PedidosRepository {
  final Dio _dio;

  PedidosRepository(this._dio);

  Future<PedidoDetalleModel> crear(CrearPedidoRequest request) async {
    try {
      final response = await _dio.post(
        '/pedidos',
        data: request.toJson(),
      );
      return PedidoDetalleModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<PedidoDetalleModel> obtener(int id) async {
    try {
      final response = await _dio.get('/pedidos/$id');
      return PedidoDetalleModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<PedidoDetalleModel> seguimiento(int id) async {
    try {
      final response = await _dio.get('/pedidos/$id/seguimiento');
      return PedidoDetalleModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<List<PedidoResumenModel>> historial() async {
    try {
      final response = await _dio.get('/pedidos/historial');
      final list = response.data as List<dynamic>;
      return list
          .map((item) =>
              PedidoResumenModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<void> cancelar(int id) async {
    try {
      await _dio.put('/pedidos/$id/cancelar');
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }
}

final pedidosRepositoryProvider = Provider<PedidosRepository>((ref) {
  final dio = ref.watch(dioProvider);
  return PedidosRepository(dio);
});
