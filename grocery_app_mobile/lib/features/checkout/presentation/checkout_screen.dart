import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../cart/presentation/providers/cart_provider.dart';
import '../../direcciones/data/models/direccion_model.dart';
import '../../direcciones/presentation/direcciones_screen.dart';
import '../../direcciones/presentation/providers/direcciones_provider.dart';
import 'pedido_confirmado_screen.dart';
import 'providers/checkout_provider.dart';

class CheckoutScreen extends ConsumerStatefulWidget {
  const CheckoutScreen({super.key});

  @override
  ConsumerState<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends ConsumerState<CheckoutScreen> {
  @override
  void initState() {
    super.initState();
    // Cargar direcciones al entrar
    Future.microtask(() => ref.invalidate(direccionesProvider));
  }

  Future<void> _seleccionarDireccion() async {
    final seleccionada = await Navigator.of(context).push<DireccionModel>(
      MaterialPageRoute(
        builder: (_) => const DireccionesScreen(modoSeleccion: true),
      ),
    );
    if (seleccionada != null) {
      ref.read(direccionSeleccionadaProvider.notifier).seleccionar(seleccionada);
    }
  }

  Future<void> _confirmarPedido() async {
    final direccion = ref.read(direccionSeleccionadaProvider);
    if (direccion == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Selecciona una dirección de entrega'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    final pedido = await ref
        .read(checkoutProvider.notifier)
        .confirmar(direccion: direccion);

    if (!mounted) return;

    if (pedido != null) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => PedidoConfirmadoScreen(pedido: pedido),
        ),
      );
    } else {
      final error = ref.read(checkoutProvider).errorMessage;
      if (error != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(error),
            backgroundColor: Colors.red.shade700,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final cartState = ref.watch(cartProvider);
    final direccion = ref.watch(direccionSeleccionadaProvider);
    final checkoutState = ref.watch(checkoutProvider);

    final tarifaEnvio = direccion?.tarifaEnvio ?? 0.0;
    final total = cartState.subtotal + tarifaEnvio;

    return Scaffold(
      appBar: AppBar(title: const Text('Confirmar Pedido')),
      body: cartState.isEmpty
          ? const Center(child: Text('Tu carrito está vacío'))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Dirección de entrega
                  _SeccionCard(
                    titulo: 'Dirección de entrega',
                    icono: Icons.location_on_outlined,
                    child: direccion == null
                        ? Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'No has seleccionado una dirección.',
                                style: TextStyle(color: Colors.black54),
                              ),
                              const SizedBox(height: 8),
                              OutlinedButton.icon(
                                onPressed: _seleccionarDireccion,
                                icon: const Icon(Icons.add_location_alt_outlined),
                                label: const Text('Seleccionar Dirección'),
                              ),
                            ],
                          )                        : Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                direccion.referencia,
                                style: const TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 15,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text('Zona: ${direccion.zonaNombre}'),
                              Text(
                                'Envío: C\$ ${direccion.tarifaEnvio.toStringAsFixed(2)}',
                                style: TextStyle(color: Colors.grey.shade700),
                              ),
                              const SizedBox(height: 8),
                              TextButton.icon(
                                onPressed: _seleccionarDireccion,
                                icon: const Icon(Icons.edit_outlined, size: 18),
                                label: const Text('Cambiar dirección'),
                              ),
                            ],
                          ),
                  ),
                  const SizedBox(height: 12),

                  // Resumen de productos
                  _SeccionCard(
                    titulo: 'Resumen del pedido',
                    icono: Icons.shopping_bag_outlined,
                    child: Column(
                      children: [
                        ...cartState.items.values.map(
                          (item) => Padding(
                            padding: const EdgeInsets.symmetric(vertical: 4),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    '${item.cantidad}x ${item.producto.nombre}',
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
                  const SizedBox(height: 12),

                  // Método de pago
                  const _SeccionCard(
                    titulo: 'Método de pago',
                    icono: Icons.payments_outlined,
                    child: Row(
                      children: [
                        Icon(Icons.money, color: Colors.green),
                        SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Efectivo contra entrega',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Totales
                  _SeccionCard(
                    titulo: 'Totales',
                    icono: Icons.receipt_long_outlined,
                    child: Column(
                      children: [
                        _FilaTotal(
                          etiqueta: 'Subtotal',
                          valor: cartState.subtotal,
                        ),
                        const SizedBox(height: 6),
                        _FilaTotal(
                          etiqueta: 'Tarifa de envío',
                          valor: tarifaEnvio,
                        ),
                        const Divider(height: 20),
                        _FilaTotal(
                          etiqueta: 'Total',
                          valor: total,
                          destacado: true,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Botón confirmar
                  SizedBox(
                    height: 52,
                    child: ElevatedButton.icon(
                      onPressed: checkoutState.enviando ? null : _confirmarPedido,
                      icon: checkoutState.enviando
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Icon(Icons.check_circle_outline),
                      label: Text(
                        checkoutState.enviando
                            ? 'Procesando...'
                            : 'Confirmar Pedido',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                ],
              ),
            ),
    );
  }
}

class _SeccionCard extends StatelessWidget {
  final String titulo;
  final IconData icono;
  final Widget child;

  const _SeccionCard({
    required this.titulo,
    required this.icono,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icono, size: 20, color: Colors.green.shade700),
                const SizedBox(width: 8),
                Text(
                  titulo,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const Divider(height: 20),
            child,
          ],
        ),
      ),
    );
  }
}

class _FilaTotal extends StatelessWidget {
  final String etiqueta;
  final double valor;
  final bool destacado;

  const _FilaTotal({
    required this.etiqueta,
    required this.valor,
    this.destacado = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          etiqueta,
          style: TextStyle(
            fontSize: destacado ? 17 : 14,
            fontWeight: destacado ? FontWeight.bold : FontWeight.normal,
          ),
        ),
        Text(
          'C\$ ${valor.toStringAsFixed(2)}',
          style: TextStyle(
            fontSize: destacado ? 20 : 14,
            fontWeight: FontWeight.bold,
            color: destacado ? Colors.green.shade700 : Colors.black87,
          ),
        ),
      ],
    );
  }
}
