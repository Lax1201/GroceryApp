import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/direcciones_repository.dart';
import '../../data/models/direccion_model.dart';

final direccionesProvider = FutureProvider<List<DireccionModel>>((ref) async {
  final repository = ref.watch(direccionesRepositoryProvider);
  return await repository.listar();
});

/// Dirección seleccionada para el checkout. Null = ninguna seleccionada.
class DireccionSeleccionadaNotifier extends Notifier<DireccionModel?> {
  @override
  DireccionModel? build() => null;

  void seleccionar(DireccionModel? direccion) => state = direccion;
}

final direccionSeleccionadaProvider =
    NotifierProvider<DireccionSeleccionadaNotifier, DireccionModel?>(
        DireccionSeleccionadaNotifier.new);
