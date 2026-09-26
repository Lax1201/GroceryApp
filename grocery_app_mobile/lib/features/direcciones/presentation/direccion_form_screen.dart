import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';
import '../data/direcciones_repository.dart';
import '../data/models/direccion_model.dart';

class DireccionFormScreen extends ConsumerStatefulWidget {
  final DireccionModel? direccion;

  const DireccionFormScreen({super.key, this.direccion});

  @override
  ConsumerState<DireccionFormScreen> createState() =>
      _DireccionFormScreenState();
}

class _DireccionFormScreenState extends ConsumerState<DireccionFormScreen> {
  // Centro por defecto: Jinotepe, Carazo, Nicaragua.
  static const LatLng _centroPorDefecto = LatLng(11.8496, -86.1993);

  final _formKey = GlobalKey<FormState>();
  final _referenciaController = TextEditingController();
  final _mapController = MapController();

  late LatLng _pinSeleccionado;
  bool _esPrincipal = false;
  bool _guardando = false;
  bool _cargandoUbicacion = false;

  bool get _esEdicion => widget.direccion != null;

  @override
  void initState() {
    super.initState();
    if (_esEdicion) {
      _pinSeleccionado = LatLng(
        widget.direccion!.latitud,
        widget.direccion!.longitud,
      );
      _referenciaController.text = widget.direccion!.referencia;
      _esPrincipal = widget.direccion!.esPrincipal;
    } else {
      _pinSeleccionado = _centroPorDefecto;
      _intentarUbicacionActual();
    }
  }

  @override
  void dispose() {
    _referenciaController.dispose();
    super.dispose();
  }

  Future<void> _intentarUbicacionActual() async {
    setState(() => _cargandoUbicacion = true);
    try {
      final servicioActivo = await Geolocator.isLocationServiceEnabled();
      if (!servicioActivo) return;

      var permiso = await Geolocator.checkPermission();
      if (permiso == LocationPermission.denied) {
        permiso = await Geolocator.requestPermission();
      }
      if (permiso == LocationPermission.denied ||
          permiso == LocationPermission.deniedForever) {
        return;
      }

      final posicion = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 10),
        ),
      );

      if (!mounted) return;
      final nueva = LatLng(posicion.latitude, posicion.longitude);
      setState(() => _pinSeleccionado = nueva);
      _mapController.move(nueva, 16);
    } catch (_) {
      // Silencioso: si falla, se queda en el centro por defecto.
    } finally {
      if (mounted) setState(() => _cargandoUbicacion = false);
    }
  }

  Future<void> _guardar() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _guardando = true);
    try {
      final repository = ref.read(direccionesRepositoryProvider);
      if (_esEdicion) {
        await repository.actualizar(
          id: widget.direccion!.id,
          latitud: _pinSeleccionado.latitude,
          longitud: _pinSeleccionado.longitude,
          referencia: _referenciaController.text,
          esPrincipal: _esPrincipal,
        );
      } else {
        await repository.crear(
          latitud: _pinSeleccionado.latitude,
          longitud: _pinSeleccionado.longitude,
          referencia: _referenciaController.text,
          esPrincipal: _esPrincipal,
        );
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(_esEdicion
              ? 'Dirección actualizada correctamente'
              : 'Dirección agregada correctamente'),
        ),
      );
      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString()),
          backgroundColor: Colors.red.shade700,
        ),
      );
    } finally {
      if (mounted) setState(() => _guardando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_esEdicion ? 'Editar Dirección' : 'Nueva Dirección'),
      ),
      body: Form(
        key: _formKey,
        child: Column(
          children: [
            // Mapa con pin seleccionable
            Expanded(
              flex: 3,
              child: Stack(
                children: [
                  FlutterMap(
                    mapController: _mapController,
                    options: MapOptions(
                      initialCenter: _pinSeleccionado,
                      initialZoom: 15,
                      onTap: (tapPosition, point) {
                        setState(() => _pinSeleccionado = point);
                      },
                    ),
                    children: [
                      TileLayer(
                        urlTemplate:
                            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                        userAgentPackageName: 'com.example.grocery_app_mobile',
                      ),
                      MarkerLayer(
                        markers: [
                          Marker(
                            point: _pinSeleccionado,
                            width: 44,
                            height: 44,
                            child: const Icon(
                              Icons.location_pin,
                              color: Colors.red,
                              size: 44,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                  if (_cargandoUbicacion)
                    const Positioned(
                      top: 12,
                      right: 12,
                      child: Card(
                        child: Padding(
                          padding: EdgeInsets.all(8),
                          child: SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          ),
                        ),
                      ),
                    ),
                  Positioned(
                    bottom: 12,
                    right: 12,
                    child: FloatingActionButton.small(
                      heroTag: 'ubicacion_actual',
                      onPressed: _intentarUbicacionActual,
                      backgroundColor: Colors.white,
                      foregroundColor: Colors.green.shade700,
                      child: const Icon(Icons.my_location),
                    ),
                  ),
                  Positioned(
                    top: 12,
                    left: 12,
                    right: 60,
                    child: Card(
                      color: Colors.white.withValues(alpha: 0.95),
                      child: const Padding(
                        padding: EdgeInsets.all(8),
                        child: Text(
                          'Toca el mapa para ajustar la ubicación exacta',
                          style: TextStyle(fontSize: 12),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Formulario
            Expanded(
              flex: 2,
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextFormField(
                      controller: _referenciaController,
                      maxLength: 300,
                      decoration: const InputDecoration(
                        labelText: 'Referencia',
                        hintText: 'Ej: Frente a la farmacia, casa azul',
                        prefixIcon: Icon(Icons.description_outlined),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'La referencia es obligatoria';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 8),
                    SwitchListTile(
                      value: _esPrincipal,
                      onChanged: (val) => setState(() => _esPrincipal = val),
                      title: const Text('Marcar como dirección principal'),
                      activeThumbColor: Colors.green.shade700,
                      contentPadding: EdgeInsets.zero,
                    ),
                    const SizedBox(height: 16),
                    SizedBox(
                      height: 48,
                      child: ElevatedButton.icon(
                        onPressed: _guardando ? null : _guardar,
                        icon: _guardando
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Icon(Icons.save_outlined),
                        label: Text(_guardando
                            ? 'Guardando...'
                            : (_esEdicion ? 'Guardar Cambios' : 'Guardar Dirección')),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
