import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../config/api_config.dart';
import '../storage/secure_storage_service.dart';
import 'auth_interceptor.dart';

class ApiClient {
  final Dio dio;

  ApiClient({required SecureStorageService storage})
      : dio = Dio(
          BaseOptions(
            baseUrl: ApiConfig.baseUrl,
            connectTimeout: ApiConfig.connectTimeout,
            receiveTimeout: ApiConfig.receiveTimeout,
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
            },
          ),
        ) {
    dio.interceptors.add(AuthInterceptor(storage));
  }
}

final apiClientProvider = Provider<ApiClient>((ref) {
  final storage = ref.watch(secureStorageServiceProvider);
  return ApiClient(storage: storage);
});

final dioProvider = Provider<Dio>((ref) {
  return ref.watch(apiClientProvider).dio;
});
