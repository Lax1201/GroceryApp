# GroceryApp

Plataforma propia de pedidos de abarrotes a domicilio para el casco urbano de Carazo, Nicaragua. No es un SaaS: es una cadena de tiendas con sucursales físicas reales que ofrece precios de mercado tradicional, pago contra entrega y catálogo orientado a la canasta básica.

**Estado actual:** Sprints 0–7 completados. Sprint 8 (integración real y hardening) pendiente.

---

## 1. Objetivo del negocio

Que una familia nicaragüense haga su mandado de abarrotes desde el celular, con precios y variedad de mercado tradicional, entregado a domicilio. El MVP arranca con **una sola sucursal** en el casco urbano de Carazo y está diseñado para escalar a más sucursales y ciudades.

**Propuesta de valor:**
- Precios de mercado tradicional, no premium.
- Pago contra entrega — sin fricción de adopción digital.
- Catálogo amplio orientado a canasta básica.

**Fuera del MVP:** ERP, POS, compras, proveedores, contabilidad, facturación, transferencias entre sucursales, inventario avanzado, pagos digitales, app propia de repartidor.

---

## 2. Arquitectura general

```text
Flutter (Android)          Blazor Server (Panel)
        │                          │
        └──────────┬───────────────┘
                   ▼
        ASP.NET Core Web API
                   │
        ┌──────────┼──────────┐
        ▼          ▼          ▼
   Application   Domain   Infrastructure
                              │
                              ▼
                          SQL Server
```

Una sola API sirve a la app Android y al panel web. Los roles (`Cliente`, `EmpleadoSucursal`, `Repartidor`, `Admin`) se resuelven vía claims en el JWT (API) o cookies (Panel).

---

## 3. Stack tecnológico

| Componente | Tecnología |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8) |
| Base de datos | SQL Server |
| ORM | Entity Framework Core 8 (Code-First) |
| Autenticación API | JWT Bearer (HMAC-SHA256) |
| Autenticación Panel | Cookies + circuito Blazor Server |
| Panel web | Blazor Server (.NET 8 InteractiveServer) |
| App móvil | Flutter 3.47 / Dart 3.13 (Android) |
| Estado Flutter | flutter_riverpod |
| HTTP Flutter | Dio + AuthInterceptor |
| Almacenamiento seguro | flutter_secure_storage |
| Mapas Flutter | flutter_map (OpenStreetMap) + latlong2 + geolocator |
| Pruebas backend | xUnit + SQLite in-memory |
| Pruebas Flutter | flutter_test |

---

## 4. Estructura de la solución

```text
GroceryApp.sln
├── GroceryApp.Domain          → entidades, enums, excepciones puras de dominio
├── GroceryApp.Application     → servicios, DTOs, interfaces, reglas de negocio, despacho
├── GroceryApp.Infrastructure  → EF Core DbContext, migraciones, seed, JwtTokenGenerator
├── GroceryApp.Api             → ASP.NET Core Web API, controllers, JWT, ProblemDetails
├── GroceryApp.Panel           → Blazor Server (admin, sucursal, repartidor)
├── GroceryApp.Tests           → xUnit + SQLite in-memory
└── grocery_app_mobile/        → App Flutter cliente (Android)
```

---

## 5. Backend (API)

API REST versionada bajo `/api/v1`. Manejo global de errores con `ProblemDetails` (RFC 7807). Rate limiting en endpoints de autenticación.

**Grupos funcionales de endpoints:**

- **Autenticación** (`/api/v1/auth`): registro y login de cliente, login de empleado.
- **Catálogo público** (`/api/v1/catalogo`): categorías, productos con filtros, detalle de producto. Sin autenticación.
- **Direcciones** (`/api/v1/direcciones`): CRUD de direcciones del cliente. Rol `Cliente`. Cálculo automático de zona al guardar el pin.
- **Pedidos** (`/api/v1/pedidos`): checkout, detalle, historial, seguimiento, cancelación. Rol `Cliente`.
- **Panel** (`/api/v1/panel`): cola de pedidos, avance de estados, asignación de repartidor, entregas, productos, sucursales. Roles `Admin`, `EmpleadoSucursal`, `Repartidor` según endpoint.

Ver `PROJECT_STATUS.md` sección 9 para el listado detallado de endpoints.

---

## 6. Panel Blazor

Panel operativo interno para `Admin`, `EmpleadoSucursal` y `Repartidor`. El cliente **no** accede al panel.

**Módulos:**
- `/login` — autenticación por cookies con redirección por rol.
- `/admin/dashboard` — métricas rápidas.
- `/admin/catalogo` — CRUD de productos, categorías, precios y stock por sucursal, subida de fotos.
- `/admin/sucursales` — supervisión de sucursales.
- `/sucursal/pedidos` — cola de pedidos de la sucursal, avance de estados, modal de detalle con recálculo por faltantes.
- `/repartidor/entregas` — entregas asignadas al repartidor autenticado.

**Despacho automático:** al pasar un pedido a `Listo`, se dispara `EntregaService.AsignarAutomaticoAsync`, que asigna al repartidor de la misma sucursal con menor carga activa. Si no hay repartidor disponible, el pedido queda en `Listo` (Pool) y se reintenta cuando un repartidor finaliza una entrega. El repartidor **no** puede tomar pedidos manualmente.

---

## 7. Aplicación Flutter (cliente)

App Android en `grocery_app_mobile/`. Arquitectura modular por features.

**Funcionalidades implementadas:**
- **Autenticación:** registro con validación telefónica (+505), login JWT, persistencia segura del token, restauración automática de sesión, logout.
- **Catálogo:** exploración pública, filtro por categoría, búsqueda reactiva, detalle de producto, imágenes servidas desde backend.
- **Carrito:** gestión local en memoria con Riverpod, cálculo reactivo de subtotales y total.
- **Direcciones:** CRUD con mapa interactivo (OpenStreetMap), pin seleccionable, GPS, referencia obligatoria, cálculo de zona y tarifa por el backend.
- **Checkout:** selección de dirección, resumen de productos, método de pago (efectivo contra entrega), tarifa de envío visible, confirmación contra `POST /api/v1/pedidos`, vaciado del carrito tras éxito.
- **Confirmación de pedido:** pantalla post-creación con número de pedido y totales.
- **Seguimiento:** línea de tiempo visual del estado del pedido con polling cada 20 segundos.
- **Historial:** lista de pedidos del cliente con acceso a seguimiento (activos) o detalle (finalizados).
- **Perfil:** accesos a direcciones, historial y cerrar sesión.

**Funcionalidades NO implementadas todavía:**
- Recuperación de contraseña.
- Notificaciones push.
- Seguimiento en tiempo real vía WebSockets/SignalR (se usa polling).
- App separada para repartidores (no está en el MVP).

---

## 8. Roles y permisos

| Rol | Acceso | Funciones |
|---|---|---|
| `Cliente` | App Flutter | Comprar, ver historial, seguir pedido, gestionar direcciones |
| `EmpleadoSucursal` | Panel Blazor | Ver/aceptar/rechazar pedidos de su sucursal, cambiar estado, marcar listo |
| `Repartidor` | Panel Blazor | Ver entregas asignadas, marcar en camino / entregado / no entregado |
| `Admin` | Panel Blazor | Gestión de catálogo, sucursales, supervisión global de pedidos |

---

## 9. Desarrollo local

### Requisitos
- .NET 8 SDK
- SQL Server (local, Docker, o remoto)
- Flutter 3.47 / Dart 3.13 (para la app móvil)
- `dotnet-ef` (opcional, para crear migraciones): `dotnet tool install --global dotnet-ef`

### Configurar cadena de conexión y JWT

```bash
cd GroceryApp.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=GroceryAppDb;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-secreta-de-al-menos-32-caracteres"
```

### Migraciones

Las migraciones se aplican automáticamente al arrancar la API (`Program.cs` ejecuta `db.Database.MigrateAsync()`). Migraciones existentes:
1. `20260725212427_InitialCreate`
2. `20260726171708_AgregarPoligonoAZona`

Para crear una nueva migración:
```bash
cd GroceryApp.Api
dotnet ef migrations add NombreMigracion --project ../GroceryApp.Infrastructure --startup-project .
```

### Seed de datos

`DbSeeder.SeedAsync` se ejecuta al arrancar la API. Siembra de forma idempotente:
- **Zonas:** "Casco urbano" con polígono WKT de Carazo.
- **Categorías:** catálogo base de categorías de abarrotes.
- **Sucursal base:** una sucursal de desarrollo.
- **Empleados de prueba:** `admin`, `operador`, `repartidor1`.

**Productos de prueba (solo Development):** `DbSeeder.SeedProductosDesarrolloAsync` siembra ~34 productos realistas de pulpería nicaragüense con `ProductoSucursal` asociado a la sucursal base. Se invoca únicamente cuando `app.Environment.IsDevelopment()` desde `Program.cs`. En Production no se siembran productos.

**Importante:** un `Producto` no aparece en el catálogo público solo por existir. Se necesita también un registro en `ProductoSucursal` con `Precio > 0` y `StockDisponible = true` para la sucursal correspondiente.

### Correr la API

```bash
dotnet run --project GroceryApp.Api
```

### Correr el Panel Blazor

```bash
dotnet run --project GroceryApp.Panel
```

### Correr la app Flutter

```bash
cd grocery_app_mobile
flutter pub get
flutter run
```

---

## 10. Comandos principales

### Backend

```bash
dotnet build GroceryApp.sln
dotnet test GroceryApp.Tests/GroceryApp.Tests.csproj
dotnet run --project GroceryApp.Api
dotnet run --project GroceryApp.Panel
```

### Flutter

```bash
cd grocery_app_mobile
flutter pub get
flutter analyze
flutter test
flutter build apk --debug
flutter run
```

---

## 11. Estado actual del proyecto

| Sprint | Foco | Estado |
|---|---|---|
| 0 | Fundación técnica | COMPLETADO |
| 1 | Backend: Autenticación | COMPLETADO |
| 2 | Backend: Catálogo y Zonas | COMPLETADO |
| 3 | Backend: Pedidos y Entregas | COMPLETADO |
| 4 | Panel Blazor + Despacho automático | COMPLETADO |
| 5 | Flutter: base + catálogo cliente | COMPLETADO |
| 6 | Flutter: direcciones + checkout | COMPLETADO |
| 7 | Flutter: seguimiento + historial + perfil | COMPLETADO |
| 8 | Integración real + hardening | PENDIENTE |

**Validaciones conocidas:**
- `dotnet build GroceryApp.sln` → 0 errores / 0 warnings
- `dotnet test` → 15/15 tests
- `flutter analyze` → sin issues
- `flutter test` → 27 tests
- `flutter build apk --debug` → exitoso

---

## 12. Próximos pasos (Sprint 8)

Según `hoja-de-ruta-app-abarrotes.md`:

> **Sprint 8 — Integración real + hardening**
> Prueba end-to-end real, rate limiting, corrección de bugs, MVP listo para clientes reales.

Pendientes de negocio que no bloquean el MVP:
- Monto exacto de la cuota de envío para municipios aledaños.
- Política definitiva ante acumulación de no-shows.
- Meta numérica de pedidos/semana para el criterio de éxito.

---

## 13. Documentación adicional

- `hoja-de-ruta-app-abarrotes.md` — Fuente única de verdad del roadmap.
- `AGENTS.md` — Reglas permanentes de arquitectura y desarrollo.
- `PROJECT_STATUS.md` — Estado detallado del proyecto.
- `AI_HANDOFF.md` — Contexto para el siguiente agente de desarrollo.


