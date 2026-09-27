import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../checkout/data/models/pedido_detalle_model.dart';
import 'providers/pedidos_provider.dart';

class SeguimientoScreen extends ConsumerStatefulWidget {
  final int pedidoId;

  const SeguimientoScreen({super.key, required this.pedidoId});

  @override
  ConsumerState<SeguimientoScreen> createState() => _SeguimientoScreenState();
}

class _SeguimientoScreenState extends ConsumerState<SeguimientoScreen> {
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    // Polling cada 20 segundos mientras la pantalla está montada.
    _pollingTimer = Timer.periodic(const Duration(seconds: 20), (_) {
      if (mounted) {
        ref.invalidate(seguimientoProvider(widget.pedidoId));
      }
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final seguimientoAsync =
        ref.watch(seguimientoProvider(widget.pedidoId));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Seguimiento del Pedido'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Actualizar',
            onPressed: () {
              ref.invalidate(seguimientoProvider(widget.pedidoId));
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(seguimientoProvider(widget.pedidoId));
        },
        child: seguimientoAsync.when(
          data: (pedido) => _ContenidoSeguimiento(pedido: pedido),
          loading: () => const Center(
            child: CircularProgressIndicator(color: Colors.green),
          ),
          error: (err, _) => Center(
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.cloud_off_rounded,
                      size: 64, color: Colors.redAccent),
                  const SizedBox(height: 16),
                  Text(err.toString(), textAlign: TextAlign.center),
                  const SizedBox(height: 16),
                  ElevatedButton.icon(
                    onPressed: () {
                      ref.invalidate(seguimientoProvider(widget.pedidoId));
                    },
                    icon: const Icon(Icons.refresh),
                    label: const Text('Reintentar'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _ContenidoSeguimiento extends StatelessWidget {
  final PedidoDetalleModel pedido;

  const _ContenidoSeguimiento({required this.pedido});

  static const List<String> _flujoPrincipal = [
    'Pendiente',
    'Confirmado',
    'EnPreparacion',
    'Listo',
    'EnCamino',
    'Entregado',
  ];

  static const Map<String, String> _etiquetas = {
    'Pendiente': 'Pedido recibido',
    'Confirmado': 'Confirmado por la sucursal',
    'EnPreparacion': 'En preparación',
    'Listo': 'Listo para entrega',
    'EnCamino': 'En camino',
    'Entregado': 'Entregado',
  };

  static const Map<String, IconData> _iconos = {
    'Pendiente': Icons.receipt_long_outlined,
    'Confirmado': Icons.check_circle_outline,
    'EnPreparacion': Icons.inventory_2_outlined,
    'Listo': Icons.shopping_bag_outlined,
    'EnCamino': Icons.delivery_dining,
    'Entregado': Icons.home_outlined,
  };

  bool get _esEstadoFinal {
    return pedido.estado == 'Entregado' ||
        pedido.estado == 'NoEntregado' ||
        pedido.estado == 'Cancelado' ||
        pedido.estado == 'Rechazado';
  }

  int get _indiceActual {
    final idx = _flujoPrincipal.indexOf(pedido.estado);
    return idx >= 0 ? idx : -1;
  }

  @override
  Widget build(BuildContext context) {
    final fecha = DateFormat('dd/MM/yyyy HH:mm').format(pedido.fechaCreacion);

    return SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Encabezado
          Card(
            elevation: 1,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Pedido #${pedido.id}',
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      _BadgeEstado(estado: pedido.estado),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.calendar_today_outlined,
                          size: 14, color: Colors.grey.shade600),
                      const SizedBox(width: 6),
                      Text(
                        fecha,
                        style: TextStyle(
                            fontSize: 13, color: Colors.grey.shade700),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Estado final (NoEntregado / Cancelado / Rechazado)
          if (_esEstadoFinal && pedido.estado != 'Entregado')
            Card(
              color: Colors.red.shade50,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: Colors.red.shade200),
              ),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Icon(Icons.info_outline, color: Colors.red.shade700),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        _mensajeEstadoFinal(pedido.estado),
                        style: TextStyle(color: Colors.red.shade900),
                      ),
                    ),
                  ],
                ),
              ),
            ),

          // Línea de tiempo
          if (!_esEstadoFinal || pedido.estado == 'Entregado')
            Card(
              elevation: 1,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Estado del pedido',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 16),
                    ...List.generate(_flujoPrincipal.length, (index) {
                      final estado = _flujoPrincipal[index];
                      final completado = index <= _indiceActual;
                      final actual = index == _indiceActual;
                      return _PasoLineaTiempo(
                        etiqueta: _etiquetas[estado] ?? estado,
                        icono: _iconos[estado] ?? Icons.circle_outlined,
                        completado: completado,
                        actual: actual,
                        esUltimo: index == _flujoPrincipal.length - 1,
                      );
                    }),
                  ],
                ),
              ),
            ),
          const SizedBox(height: 16),

          // Dirección de entrega
          Card(
            elevation: 1,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.location_on_outlined,
                          size: 20, color: Colors.green.shade700),
                      const SizedBox(width: 8),
                      const Text(
                        'Dirección de entrega',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(pedido.direccionReferencia),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Resumen de productos
          Card(
            elevation: 1,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.shopping_bag_outlined,
                          size: 20, color: Colors.green.shade700),
                      const SizedBox(width: 8),
                      const Text(
                        'Productos',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                  const Divider(height: 20),
                  ...pedido.items.map(
                    (item) => Padding(
                      padding: const EdgeInsets.symmetric(vertical: 4),
                      child: Row(
                        children: [
                          Expanded(
                            child: Text(
                              '${item.cantidad}x ${item.productoNombre}',
                              style: const TextStyle(fontSize: 14),
                            ),
                          ),
                          Text(
                            'C\$ ${item.subtotal.toStringAsFixed(2)}',
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Totales
          Card(
            elevation: 1,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Subtotal'),
                      Text('C\$ ${pedido.subtotal.toStringAsFixed(2)}'),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Tarifa de envío'),
                      Text('C\$ ${pedido.tarifaEnvio.toStringAsFixed(2)}'),
                    ],
                  ),
                  const Divider(height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Total',
                        style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Text(
                        'C\$ ${pedido.total.toStringAsFixed(2)}',
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                          color: Colors.green.shade700,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  String _mensajeEstadoFinal(String estado) {
    switch (estado) {
      case 'NoEntregado':
        return 'El pedido no pudo ser entregado. El repartidor no encontró a nadie en la dirección.';
      case 'Cancelado':
        return 'Este pedido fue cancelado.';
      case 'Rechazado':
        return 'La sucursal rechazó este pedido.';
      default:
        return 'El pedido finalizó con estado: $estado.';
    }
  }
}

class _BadgeEstado extends StatelessWidget {
  final String estado;

  const _BadgeEstado({required this.estado});

  Color _color(String estado) {
    switch (estado) {
      case 'Pendiente':
        return Colors.orange;
      case 'Confirmado':
        return Colors.blue;
      case 'EnPreparacion':
        return Colors.indigo;
      case 'Listo':
        return Colors.teal;
      case 'EnCamino':
        return Colors.purple;
      case 'Entregado':
        return Colors.green;
      case 'NoEntregado':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  String _etiqueta(String estado) {
    switch (estado) {
      case 'EnPreparacion':
        return 'En preparación';
      case 'Listo':
        return 'Listo';
      case 'EnCamino':
        return 'En camino';
      case 'NoEntregado':
        return 'No entregado';
      default:
        return estado;
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _color(estado);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.5)),
      ),
      child: Text(
        _etiqueta(estado),
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.bold,
          color: color,
        ),
      ),
    );
  }
}

class _PasoLineaTiempo extends StatelessWidget {
  final String etiqueta;
  final IconData icono;
  final bool completado;
  final bool actual;
  final bool esUltimo;

  const _PasoLineaTiempo({
    required this.etiqueta,
    required this.icono,
    required this.completado,
    required this.actual,
    required this.esUltimo,
  });

  @override
  Widget build(BuildContext context) {
    final color = completado ? Colors.green.shade700 : Colors.grey.shade400;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 32,
                height: 32,
                decoration: BoxDecoration(
                  color: completado ? Colors.green.shade700 : Colors.grey.shade200,
                  shape: BoxShape.circle,
                  border: actual
                      ? Border.all(color: Colors.green.shade900, width: 3)
                      : null,
                ),
                child: Icon(
                  icono,
                  size: 18,
                  color: completado ? Colors.white : Colors.grey.shade600,
                ),
              ),
              if (!esUltimo)
                Expanded(
                  child: Container(
                    width: 2,
                    color: color,
                  ),
                ),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(top: 6, bottom: esUltimo ? 0 : 20),
              child: Text(
                etiqueta,
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: actual ? FontWeight.bold : FontWeight.normal,
                  color: completado ? Colors.black87 : Colors.grey.shade600,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
