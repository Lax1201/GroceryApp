import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/catalog_repository.dart';
import '../../data/models/category_model.dart';
import '../../data/models/product_model.dart';

final categoriesProvider = FutureProvider<List<CategoriaModel>>((ref) async {
  final repository = ref.watch(catalogRepositoryProvider);
  return await repository.getCategories();
});

class SelectedCategoryIdNotifier extends Notifier<int?> {
  @override
  int? build() => null;

  void setSelected(int? id) => state = id;
}

final selectedCategoryIdProvider =
    NotifierProvider<SelectedCategoryIdNotifier, int?>(SelectedCategoryIdNotifier.new);

class SearchQueryNotifier extends Notifier<String> {
  @override
  String build() => '';

  void setSearch(String query) => state = query;
}

final searchQueryProvider =
    NotifierProvider<SearchQueryNotifier, String>(SearchQueryNotifier.new);

final catalogProductsProvider = FutureProvider<List<ProductoModel>>((ref) async {
  final repository = ref.watch(catalogRepositoryProvider);
  final categoryId = ref.watch(selectedCategoryIdProvider);
  final search = ref.watch(searchQueryProvider);

  return await repository.getProducts(
    categoryId: categoryId,
    search: search.isNotEmpty ? search : null,
  );
});

final productDetailProvider =
    FutureProvider.family<ProductoModel, int>((ref, id) async {
  final repository = ref.watch(catalogRepositoryProvider);
  return await repository.getProductById(id);
});
