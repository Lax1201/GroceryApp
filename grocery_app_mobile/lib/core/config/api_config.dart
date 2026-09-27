class ApiConfig {
  ApiConfig._();

  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://192.168.0.2:5000/api/v1',
  );

  static const Duration connectTimeout = Duration(seconds: 10);
  static const Duration receiveTimeout = Duration(seconds: 10);

  /// Resuelve la URL completa para imágenes retornadas por el backend.
  /// Si ya es una URL absoluta (http/https), la devuelve intacta.
  /// Si es una ruta relativa (ej. /uploads/productos/x.jpg), le antepone el host base.
  static String? resolveImageUrl(String? relativeOrAbsoluteUrl) {
    if (relativeOrAbsoluteUrl == null || relativeOrAbsoluteUrl.trim().isEmpty) {
      return null;
    }
    final trimmed = relativeOrAbsoluteUrl.trim();
    if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
      return trimmed;
    }

    final uri = Uri.parse(baseUrl);
    final hostPort = uri.hasPort ? '${uri.host}:${uri.port}' : uri.host;
    final scheme = uri.scheme.isNotEmpty ? uri.scheme : 'http';
    final normalizedPath = trimmed.startsWith('/') ? trimmed : '/$trimmed';

    return '$scheme://$hostPort$normalizedPath';
  }
}
