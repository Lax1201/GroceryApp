import 'package:flutter/material.dart';
import '../../pedidos/presentation/seguimiento_screen.dart';
import '../data/models/pedido_detalle_model.dart';

class PedidoConfirmadoScreen extends StatelessWidget {
  final PedidoDetalleModel pedido;

  const PedidoConfirmadoScreen({super.key, required this.pedido});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Pedido Confirmado'),
        automaticallyImplyLeading: false,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              const SizedBox(height: 24),
              Icon(
                Icons.check_circle,
                size: 96,
                color: Colors.green.shade600,
              ),
              const SizedBox(height: 16),
              const Text(
                '¡Pedido creado con éxito!',
                style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                'Pedido #${pedido.id}',
                style: TextStyle(fontSize: 16, color: Colors.grey.shade700),
              ),
              const SizedBox(height: 24),

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
                      const Divider(height: 24),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('Subtotal'),
                          Text('C\$ ${pedido.subtotal.toStringAsFixed(2)}'),
                        ],
                      ),
                      const SizedBox(height: 4),
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

              const Spacer(),

              SizedBox(
                width: double.infinity,
                height: 48,
                child: ElevatedButton.icon(
                  onPressed: () {
                    Navigator.of(context).pushReplacement(
                      MaterialPageRoute(
                        builder: (_) => SeguimientoScreen(pedidoId: pedido.id),
                      ),
                    );
                  },
                  icon: const Icon(Icons.local_shipping_outlined),
                  label: const Text('Ver Seguimiento'),
                ),
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                height: 48,
                child: OutlinedButton.icon(
                  onPressed: () {
                    Navigator.of(context).pushNamedAndRemoveUntil(
                      '/catalog',
                      (route) => false,
                    );
                  },
                  icon: const Icon(Icons.storefront),
                  label: const Text('Volver al Catálogo'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
