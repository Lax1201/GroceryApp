class CategoriaModel {
  final int id;
  final String nombre;

  const CategoriaModel({
    required this.id,
    required this.nombre,
  });

  factory CategoriaModel.fromJson(Map<String, dynamic> json) {
    return CategoriaModel(
      id: json['id'] as int? ?? 0,
      nombre: json['nombre'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'nombre': nombre,
      };

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is CategoriaModel && runtimeType == other.runtimeType && id == other.id;

  @override
  int get hashCode => id.hashCode;
}
