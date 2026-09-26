import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_client.dart';
import 'models/crear_pedido_request.dart';
import 'models/pedido_detalle_model.dart';

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
}

final pedidosRepositoryProvider = Provider<PedidosRepository>((ref) {
  final dio = ref.watch(dioProvider);
  return PedidosRepository(dio);
});
