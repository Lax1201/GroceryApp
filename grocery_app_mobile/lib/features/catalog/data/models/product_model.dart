class ProductoModel {
  final int id;
  final String nombre;
  final String? descripcion;
  final int categoriaId;
  final String categoriaNombre;
  final String? fotoUrl;
  final double precio;
  final bool stockDisponible;

  const ProductoModel({
    required this.id,
    required this.nombre,
    this.descripcion,
    required this.categoriaId,
    required this.categoriaNombre,
    this.fotoUrl,
    required this.precio,
    required this.stockDisponible,
  });

  factory ProductoModel.fromJson(Map<String, dynamic> json) {
    return ProductoModel(
      id: json['id'] as int? ?? 0,
      nombre: json['nombre'] as String? ?? '',
      descripcion: json['descripcion'] as String?,
      categoriaId: json['categoriaId'] as int? ?? 0,
      categoriaNombre: json['categoriaNombre'] as String? ?? '',
      fotoUrl: json['fotoUrl'] as String?,
      precio: (json['precio'] as num?)?.toDouble() ?? 0.0,
      stockDisponible: json['stockDisponible'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'nombre': nombre,
        'descripcion': descripcion,
        'categoriaId': categoriaId,
        'categoriaNombre': categoriaNombre,
        'fotoUrl': fotoUrl,
        'precio': precio,
        'stockDisponible': stockDisponible,
      };

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ProductoModel && runtimeType == other.runtimeType && id == other.id;

  @override
  int get hashCode => id.hashCode;
}
