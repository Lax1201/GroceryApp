import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/config/api_config.dart';
import '../../auth/presentation/providers/auth_provider.dart';
import '../../cart/presentation/providers/cart_provider.dart';
import '../data/models/product_model.dart';
import 'product_detail_screen.dart';
import 'providers/catalog_provider.dart';

class CatalogScreen extends ConsumerStatefulWidget {
  const CatalogScreen({super.key});

  @override
  ConsumerState<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends ConsumerState<CatalogScreen> {
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authProvider);
    final cartState = ref.watch(cartProvider);
    final categoriesAsync = ref.watch(categoriesProvider);
    final productsAsync = ref.watch(catalogProductsProvider);
    final selectedCategoryId = ref.watch(selectedCategoryIdProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.shopping_bag, color: Colors.green),
            SizedBox(width: 8),
            Text('GroceryApp', style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        actions: [
          // Botón del carrito con badge de cantidad
          IconButton(
            icon: Badge(
              isLabelVisible: cartState.totalItemCount > 0,
              label: Text('${cartState.totalItemCount}'),
              child: const Icon(Icons.shopping_cart_outlined),
            ),
            tooltip: 'Ver Carrito',
            onPressed: () {
              Navigator.of(context).pushNamed('/cart');
            },
          ),
          // Botón de perfil
          IconButton(
            icon: const Icon(Icons.person_outline),
            tooltip: 'Mi Perfil',
            onPressed: () {
              Navigator.of(context).pushNamed('/perfil');
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(categoriesProvider);
          ref.invalidate(catalogProductsProvider);
        },
        child: Column(
          children: [
            // Barra de búsqueda
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
              child: TextField(
                controller: _searchController,
                decoration: InputDecoration(
                  hintText: 'Buscar productos...',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: _searchController.text.isNotEmpty
                      ? IconButton(
                          icon: const Icon(Icons.clear),
                          onPressed: () {
                            _searchController.clear();
                            ref.read(searchQueryProvider.notifier).setSearch('');
                          },
                        )
                      : null,
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(24),
                    borderSide: BorderSide(color: Colors.grey.shade300),
                  ),
                  filled: true,
                  fillColor: Colors.grey.shade100,
                ),
                onChanged: (val) {
                  ref.read(searchQueryProvider.notifier).setSearch(val);
                },
              ),
            ),

            // Selector horizontal de Categorías
            categoriesAsync.when(
              data: (categories) {
                return SizedBox(
                  height: 44,
                  child: ListView.separated(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    scrollDirection: Axis.horizontal,
                    itemCount: categories.length + 1,
                    separatorBuilder: (_, _) => const SizedBox(width: 8),
                    itemBuilder: (context, index) {
                      final isAll = index == 0;
                      final isSelected = isAll
                          ? selectedCategoryId == null
                          : selectedCategoryId == categories[index - 1].id;
                      final label = isAll ? 'Todas' : categories[index - 1].nombre;

                      return ChoiceChip(
                        label: Text(label),
                        selected: isSelected,
                        selectedColor: Colors.green.shade100,
                        labelStyle: TextStyle(
                          color: isSelected ? Colors.green.shade900 : Colors.black87,
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                        ),
                        onSelected: (selected) {
                          if (isAll) {
                            ref.read(selectedCategoryIdProvider.notifier).setSelected(null);
                          } else {
                            ref.read(selectedCategoryIdProvider.notifier).setSelected(
                                selected ? categories[index - 1].id : null);
                          }
                        },
                      );
                    },
                  ),
                );
              },
              loading: () => const SizedBox(
                height: 44,
                child: Center(
                  child: SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                ),
              ),
              error: (_, _) => const SizedBox.shrink(),
            ),

            const SizedBox(height: 8),

            // Cuadrícula de Productos
            Expanded(
              child: productsAsync.when(
                data: (products) {
                  if (products.isEmpty) {
                    return Center(
                      child: SingleChildScrollView(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.all(24),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.inventory_2_outlined, size: 64, color: Colors.grey.shade400),
                            const SizedBox(height: 16),
                            const Text(
                              'No se encontraron productos',
                              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Prueba ajustando los filtros o el término de búsqueda.',
                              textAlign: TextAlign.center,
                              style: TextStyle(color: Colors.black54),
                            ),
                          ],
                        ),
                      ),
                    );
                  }

                  return GridView.builder(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(12),
                    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      childAspectRatio: 0.70,
                      crossAxisSpacing: 10,
                      mainAxisSpacing: 10,
                    ),
                    itemCount: products.length,
                    itemBuilder: (context, index) {
                      final product = products[index];
                      return _ProductCard(
                        product: product,
                        onTap: () {
                          Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (_) => ProductDetailScreen(productId: product.id),
                            ),
                          );
                        },
                      );
                    },
                  );
                },
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
                        const Icon(Icons.cloud_off_rounded, size: 64, color: Colors.redAccent),
                        const SizedBox(height: 16),
                        Text(
                          err.toString(),
                          textAlign: TextAlign.center,
                          style: const TextStyle(fontSize: 16),
                        ),
                        const SizedBox(height: 16),
                        ElevatedButton.icon(
                          onPressed: () {
                            ref.invalidate(catalogProductsProvider);
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
          ],
        ),
      ),
    );
  }
}

class _ProductCard extends ConsumerWidget {
  final ProductoModel product;
  final VoidCallback onTap;

  const _ProductCard({
    required this.product,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final fullImageUrl = ApiConfig.resolveImageUrl(product.fotoUrl);

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Imagen del producto
            Expanded(
              child: Container(
                color: Colors.grey.shade100,
                child: fullImageUrl != null
                    ? Image.network(
                        fullImageUrl,
                        fit: BoxFit.cover,
                        loadingBuilder: (context, child, loadingProgress) {
                          if (loadingProgress == null) return child;
                          return const Center(
                            child: SizedBox(
                              height: 24,
                              width: 24,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            ),
                          );
                        },
                        errorBuilder: (context, error, stackTrace) {
                          return const Center(
                            child: Icon(Icons.broken_image, size: 40, color: Colors.grey),
                          );
                        },
                      )
                    : const Center(
                        child: Icon(
                          Icons.shopping_basket_outlined,
                          size: 44,
                          color: Colors.grey,
                        ),
                      ),
              ),
            ),
            // Detalles
            Padding(
              padding: const EdgeInsets.all(8.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    product.categoriaNombre,
                    style: TextStyle(
                      fontSize: 11,
                      color: Colors.grey.shade600,
                      fontWeight: FontWeight.w500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    product.nombre,
                    style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'C\$ ${product.precio.toStringAsFixed(2)}',
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: Colors.green,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: product.stockDisponible
                              ? Colors.green.shade50
                              : Colors.red.shade50,
                          borderRadius: BorderRadius.circular(4),
                          border: Border.all(
                            color: product.stockDisponible
                                ? Colors.green.shade300
                                : Colors.red.shade300,
                          ),
                        ),
                        child: Text(
                          product.stockDisponible ? 'Stock' : 'Agotado',
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: product.stockDisponible
                                ? Colors.green.shade800
                                : Colors.red.shade800,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: product.stockDisponible
                          ? () {
                              ref.read(cartProvider.notifier).agregarProducto(product);
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text('Agregado: ${product.nombre}'),
                                  duration: const Duration(seconds: 1),
                                  action: SnackBarAction(
                                    label: 'Ver Carrito',
                                    onPressed: () {
                                      Navigator.of(context).pushNamed('/cart');
                                    },
                                  ),
                                ),
                              );
                            }
                          : null,
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        backgroundColor: Colors.green,
                        foregroundColor: Colors.white,
                        disabledBackgroundColor: Colors.grey.shade300,
                        visualDensity: VisualDensity.compact,
                      ),
                      icon: const Icon(Icons.add_shopping_cart, size: 16),
                      label: const Text('Agregar', style: TextStyle(fontSize: 12)),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
