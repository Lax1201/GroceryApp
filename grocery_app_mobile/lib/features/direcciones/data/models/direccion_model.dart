class DireccionModel {
  final int id;
  final double latitud;
  final double longitud;
  final String referencia;
  final bool esPrincipal;
  final String zonaNombre;
  final double tarifaEnvio;

  const DireccionModel({
    required this.id,
    required this.latitud,
    required this.longitud,
    required this.referencia,
    required this.esPrincipal,
    required this.zonaNombre,
    required this.tarifaEnvio,
  });

  factory DireccionModel.fromJson(Map<String, dynamic> json) {
    return DireccionModel(
      id: json['id'] as int? ?? 0,
      latitud: (json['latitud'] as num?)?.toDouble() ?? 0.0,
      longitud: (json['longitud'] as num?)?.toDouble() ?? 0.0,
      referencia: json['referencia'] as String? ?? '',
      esPrincipal: json['esPrincipal'] as bool? ?? false,
      zonaNombre: json['zonaNombre'] as String? ?? '',
      tarifaEnvio: (json['tarifaEnvio'] as num?)?.toDouble() ?? 0.0,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'latitud': latitud,
        'longitud': longitud,
        'referencia': referencia,
        'esPrincipal': esPrincipal,
        'zonaNombre': zonaNombre,
        'tarifaEnvio': tarifaEnvio,
      };

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is DireccionModel && runtimeType == other.runtimeType && id == other.id;

  @override
  int get hashCode => id.hashCode;
}
