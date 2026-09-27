import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/presentation/providers/auth_provider.dart';

class PerfilScreen extends ConsumerWidget {
  const PerfilScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final autenticado = authState.status == AuthStatus.authenticated;

    return Scaffold(
      appBar: AppBar(title: const Text('Mi Perfil')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Encabezado
          Card(
            elevation: 1,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 32,
                    backgroundColor: Colors.green.shade100,
                    child: Icon(
                      Icons.person,
                      size: 36,
                      color: Colors.green.shade800,
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          autenticado ? 'Cliente' : 'Invitado',
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          autenticado
                              ? 'Sesión activa'
                              : 'Inicia sesión para más funciones',
                          style: TextStyle(
                            fontSize: 13,
                            color: Colors.grey.shade700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Opciones
          Card(
            elevation: 1,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(
              children: [
                ListTile(
                  leading: Icon(Icons.location_on_outlined,
                      color: Colors.green.shade700),
                  title: const Text('Mis Direcciones'),
                  subtitle: const Text('Gestiona tus direcciones de entrega'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () {
                    if (!autenticado) {
                      _pedirLogin(context);
                      return;
                    }
                    Navigator.of(context).pushNamed('/direcciones');
                  },
                ),
                const Divider(height: 1),
                ListTile(
                  leading: Icon(Icons.receipt_long_outlined,
                      color: Colors.green.shade700),
                  title: const Text('Historial de Pedidos'),
                  subtitle: const Text('Revisa tus pedidos anteriores'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () {
                    if (!autenticado) {
                      _pedirLogin(context);
                      return;
                    }
                    Navigator.of(context).pushNamed('/historial');
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // Sesión
          if (autenticado)
            Card(
              elevation: 1,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
              child: ListTile(
                leading: const Icon(Icons.logout, color: Colors.red),
                title: const Text(
                  'Cerrar Sesión',
                  style: TextStyle(color: Colors.red),
                ),
                onTap: () async {
                  final confirm = await showDialog<bool>(
                    context: context,
                    builder: (ctx) => AlertDialog(
                      title: const Text('Cerrar sesión'),
                      content:
                          const Text('¿Estás seguro de que deseas salir?'),
                      actions: [
                        TextButton(
                          onPressed: () => Navigator.of(ctx).pop(false),
                          child: const Text('Cancelar'),
                        ),
                        TextButton(
                          onPressed: () => Navigator.of(ctx).pop(true),
                          child: const Text(
                            'Salir',
                            style: TextStyle(color: Colors.red),
                          ),
                        ),
                      ],
                    ),
                  );
                  if (confirm == true) {
                    await ref.read(authProvider.notifier).logout();
                    if (context.mounted) {
                      Navigator.of(context)
                          .pushReplacementNamed('/catalog');
                    }
                  }
                },
              ),
            )
          else
            SizedBox(
              height: 48,
              child: ElevatedButton.icon(
                onPressed: () {
                  Navigator.of(context).pushReplacementNamed('/login');
                },
                icon: const Icon(Icons.login),
                label: const Text('Iniciar Sesión'),
              ),
            ),
        ],
      ),
    );
  }

  void _pedirLogin(BuildContext context) {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Inicia sesión para acceder a esta sección'),
        backgroundColor: Colors.orange,
      ),
    );
  }
}
