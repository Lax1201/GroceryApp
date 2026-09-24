import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_client.dart';
import 'models/category_model.dart';
import 'models/product_model.dart';

class CatalogRepository {
  final Dio _dio;

  CatalogRepository(this._dio);

  Future<List<CategoriaModel>> getCategories() async {
    try {
      final response = await _dio.get('/catalogo/categorias');
      final list = response.data as List<dynamic>;
      return list
          .map((item) => CategoriaModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<List<ProductoModel>> getProducts({
    int? categoryId,
    String? search,
  }) async {
    try {
      final queryParameters = <String, dynamic>{};
      if (categoryId != null && categoryId > 0) {
        queryParameters['categoriaId'] = categoryId;
      }
      if (search != null && search.trim().isNotEmpty) {
        queryParameters['busqueda'] = search.trim();
      }

      final response = await _dio.get(
        '/catalogo/productos',
        queryParameters: queryParameters,
      );
      final list = response.data as List<dynamic>;
      return list
          .map((item) => ProductoModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }

  Future<ProductoModel> getProductById(int id) async {
    try {
      final response = await _dio.get('/catalogo/productos/$id');
      return ProductoModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException(e.toString());
    }
  }
}

final catalogRepositoryProvider = Provider<CatalogRepository>((ref) {
  final dio = ref.watch(dioProvider);
  return CatalogRepository(dio);
});
