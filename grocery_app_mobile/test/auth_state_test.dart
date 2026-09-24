import 'package:flutter_test/flutter_test.dart';
import 'package:grocery_app_mobile/features/auth/presentation/providers/auth_provider.dart';

void main() {
  group('AuthState Unit Tests', () {
    test('AuthState initial state has correct defaults', () {
      const state = AuthState.initial();

      expect(state.status, AuthStatus.initial);
      expect(state.token, isNull);
      expect(state.errorMessage, isNull);
    });

    test('AuthState copyWith updates status and token', () {
      const state = AuthState.initial();
      final updated = state.copyWith(
        status: AuthStatus.authenticated,
        token: 'sample_jwt_token',
      );

      expect(updated.status, AuthStatus.authenticated);
      expect(updated.token, 'sample_jwt_token');
      expect(updated.errorMessage, isNull);
    });

    test('AuthState copyWith preserves or clears error properly', () {
      const state = AuthState(
        status: AuthStatus.unauthenticated,
        errorMessage: 'Error previo',
      );

      final withNewStatus = state.copyWith(status: AuthStatus.loading);
      expect(withNewStatus.errorMessage, 'Error previo');

      final cleared = state.copyWith(clearError: true);
      expect(cleared.errorMessage, isNull);
    });
  });
}
