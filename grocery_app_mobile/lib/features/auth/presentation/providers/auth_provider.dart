import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../data/auth_repository.dart';

enum AuthStatus {
  initial,
  loading,
  authenticated,
  unauthenticated,
}

class AuthState {
  final AuthStatus status;
  final String? token;
  final String? errorMessage;

  const AuthState({
    required this.status,
    this.token,
    this.errorMessage,
  });

  const AuthState.initial()
      : status = AuthStatus.initial,
        token = null,
        errorMessage = null;

  AuthState copyWith({
    AuthStatus? status,
    String? token,
    String? errorMessage,
    bool clearError = false,
  }) {
    return AuthState(
      status: status ?? this.status,
      token: token ?? this.token,
      errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
    );
  }
}

class AuthNotifier extends Notifier<AuthState> {
  @override
  AuthState build() {
    Future.microtask(() => checkAuthStatus());
    return const AuthState.initial();
  }

  AuthRepository get _repository => ref.read(authRepositoryProvider);
  SecureStorageService get _storage => ref.read(secureStorageServiceProvider);

  Future<void> checkAuthStatus() async {
    try {
      final token = await _storage.getToken();
      if (token != null && token.isNotEmpty) {
        state = AuthState(
          status: AuthStatus.authenticated,
          token: token,
        );
      } else {
        state = const AuthState(status: AuthStatus.unauthenticated);
      }
    } catch (_) {
      state = const AuthState(status: AuthStatus.unauthenticated);
    }
  }

  Future<bool> login({required String telefono, required String password}) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final response = await _repository.login(
        telefono: telefono,
        password: password,
      );
      await _storage.saveToken(response.token);
      state = AuthState(
        status: AuthStatus.authenticated,
        token: response.token,
      );
      return true;
    } catch (e) {
      state = state.copyWith(
        status: AuthStatus.unauthenticated,
        errorMessage: e.toString(),
      );
      return false;
    }
  }

  Future<bool> register({
    required String nombre,
    required String telefono,
    required String password,
    String? email,
  }) async {
    state = state.copyWith(status: AuthStatus.loading, clearError: true);
    try {
      final response = await _repository.register(
        nombre: nombre,
        telefono: telefono,
        password: password,
        email: email,
      );
      await _storage.saveToken(response.token);
      state = AuthState(
        status: AuthStatus.authenticated,
        token: response.token,
      );
      return true;
    } catch (e) {
      state = state.copyWith(
        status: AuthStatus.unauthenticated,
        errorMessage: e.toString(),
      );
      return false;
    }
  }

  Future<void> logout() async {
    await _storage.deleteToken();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  void clearError() {
    state = state.copyWith(clearError: true);
  }
}

final authProvider = NotifierProvider<AuthNotifier, AuthState>(AuthNotifier.new);
