# AI Handoff — GroceryApp

## 1. Estado actual
* **Proyecto:** GroceryApp — Sistema propio de grocery delivery.
* **Sprint actual:** Sprint 5 COMPLETADO.
* **Rama de desarrollo activa:** `dev`.
* **Rama `main`:** Contiene el estado actualizado hasta Sprint 5.
* **Próximo sprint:** Determinado exclusivamente por `hoja-de-ruta-app-abarrotes.md` (Sprint 6).

## 2. Fuentes de verdad
1. `hoja-de-ruta-app-abarrotes.md` — Fuente oficial del roadmap.
2. `AGENTS.md` — Reglas permanentes de desarrollo y arquitectura.
3. `PROJECT_STATUS.md` — Estado del proyecto y registro de avances.
4. `README.md` — Documentación y resumen general.

## 3. Arquitectura
* **Backend:**
  * .NET 8 / C# con ASP.NET Core Web API.
  * Capas: `Domain`, `Application`, `Infrastructure`, `Api`.
  * Entity Framework Core 8 con SQL Server.
  * Autenticación con JWT Bearer.
  * Endpoints versionados bajo `/api/v1`.
* **Panel Web:**
  * Blazor Server (.NET 8 InteractiveServer).
  * Uso operativo para Administración, Sucursal y Repartidores.
* **Cliente Móvil:**
  * Flutter Android (`grocery_app_mobile`).
  * Gestión de estado con Flutter Riverpod.
  * Cliente HTTP con Dio + AuthInterceptor.
  * Almacenamiento seguro con `flutter_secure_storage`.
  * Arquitectura modular por features (`core`, `features/auth`, `features/catalog`, `features/cart`).

## 4. Sprint 4 / Despacho
* Los repartidores **NO** toman pedidos manualmente (sin autoservicio).
* La asignación es **automática** inmediatamente cuando el pedido pasa a `Listo`.
* La asignación se realiza dentro de la **misma sucursal** del pedido.
* Estrategia activa: **menor carga activa** en estados `Asignado` o `EnCamino`.
* Existe asignación manual únicamente como fallback para roles autorizados (`Admin`, `EmpleadoSucursal`).
* **No existe** una app Flutter separada para repartidores en el MVP (operan desde el panel web Blazor).

## 5. Sprint 5 COMPLETADO
### Flutter (`grocery_app_mobile`)
* Arquitectura base `core` y `features`.
* Autenticación de cliente: Registro (teléfono +505 de 8 dígitos), Login JWT, almacenamiento seguro de token, restauración automática de sesión y Logout.
* Catálogo público: Exploración sin login obligatorio, categorías dinámicas en chips, búsqueda de texto, listado de productos, vista de detalle e imágenes servidas desde backend.
* Carrito de compras local en memoria (`CartNotifier` con Riverpod) con cálculo reactivo de subtotales y total en Córdobas (C$). No crea pedidos aún.

### Backend
* Endpoints de catálogo público implementados y protegidos:
  * `GET /api/v1/catalogo/categorias`
  * `GET /api/v1/catalogo/productos` (soporta filtros `categoriaId` y `busqueda`)
  * `GET /api/v1/catalogo/productos/{id}`
* El backend es la única autoridad sobre precios, stock y disponibilidad.
* Flutter **nunca** accede directamente a la base de datos (todo es vía REST API).
* El carrito aún **NO** genera pedidos (alcance de Sprint 6).

## 6. Validación conocida de Sprint 5
Validaciones ejecutadas y confirmadas:
```text
dotnet build: PASS
dotnet test: PASS — 15 tests
flutter analyze: PASS
flutter test: PASS — 15 tests
flutter build apk --debug: PASS
```
*(No reejecutar estas validaciones sin motivo).*

## 7. Indicaciones para el Próximo Agente
Antes de desarrollar:
1. Leer `AGENTS.md`.
2. Leer `hoja-de-ruta-app-abarrotes.md`.
3. Leer este archivo `AI_HANDOFF.md`.
4. Revisar el estado actual del código relacionado con el sprint entrante.
5. Continuar desde el estado existente sin rehacer Sprint 5.
6. **No diseñar Sprint 6 dentro de este documento**; el roadmap oficial determina qué corresponde implementar.

## 8. Reglas de Continuidad
* Respetar rigurosamente la arquitectura existente.
* Evitar sobreingeniería.
* No inventar endpoints ni modelos fuera del estándar.
* No duplicar funcionalidades ya existentes.
* No alterar reglas operativas previas sin necesidad.
* Mantener total compatibilidad con Android en Flutter.
* Consumir exclusivamente la REST API bajo `/api/v1`.
* Ejecutar la suite de validaciones al finalizar el trabajo del sprint.
* **No hacer push automáticamente.**

## 9. Estado Git
```text
Trabajo actual: dev
Sprint 5: completado
Main: actualizado hasta Sprint 5
Handoff: preparado para siguiente agente
```
