# GroceryApp — Estado del Proyecto (Project Status)

**Fecha de corte:** Septiembre 2026  
**Hito completado:** Sprint 4 — Panel Web Blazor Server y Modelo de Despacho Automático  
**Próximo hito:** Sprint 5 — Aplicación Cliente en Flutter (Base y Catálogo)

---

## 1. Resumen Ejecutivo del Estado

El proyecto **GroceryApp** ha completado con éxito la fase fundacional del backend y su panel web de administración y operación interna. La plataforma cuenta con una API REST funcional, una base de datos relacional con migraciones activas en Entity Framework Core, un panel web administrativo e interactivo en Blazor Server con control de acceso por roles y sucursal, un motor de despacho con asignación automática de pedidos a repartidores desacoplado, y una suite de pruebas automatizadas con cobertura total de los escenarios críticos de despacho y concurrencia.

### Estado de los Sprints
* **Sprint 0 — Fundación técnica:** COMPLETADO
* **Sprint 1 — Autenticación:** COMPLETADO
* **Sprint 2 — Catálogo y zonas:** COMPLETADO
* **Sprint 3 — Pedidos y entregas (Core Backend):** COMPLETADO
* **Sprint 4 — Panel Web Blazor Server y Despacho Automático:** COMPLETADO
* **Sprint 5 — Flutter: base y catálogo móvil:** PENDIENTE
* **Sprint 6 — Flutter: direcciones y checkout:** PENDIENTE
* **Sprint 7 — Flutter: seguimiento e historial:** PENDIENTE
* **Sprint 8 — Integración final y hardening:** PENDIENTE

---

## 2. Arquitectura de la Solución

La solución mantiene una arquitectura limpia desacoplada en 6 proyectos interconectados:

```text
GroceryApp.sln
├── GroceryApp.Domain          (Entidades de negocio, enums, excepciones puras de dominio)
├── GroceryApp.Application     (Servicios, DTOs, interfaces, reglas de negocio y despacho)
├── GroceryApp.Infrastructure  (EF Core DbContext, persistencia SQL Server, migraciones, JWT)
├── GroceryApp.Api             (ASP.NET Core Web API RESTful, JWT Bearer, Swagger)
├── GroceryApp.Panel           (Blazor Server .NET 8, Auth por cookies, componentes UI operativos)
└── GroceryApp.Tests           (Suite de pruebas xUnit, SQLite in-memory, verificación de concurrencia)
```

### Responsabilidad por Proyecto
1. **`GroceryApp.Domain`**:
   * Entidades centrales: `Cliente`, `Empleado`, `Sucursal`, `Producto`, `ProductoSucursal`, `Categoria`, `Pedido`, `PedidoItem`, `Entrega`, `Zona`, `Direccion`.
   * Enums legibles almacenados como texto: `RolEmpleado`, `TipoZona`, `EstadoPedido`, `EstadoEntrega`.
   * Transiciones de estado encapsuladas en métodos de dominio con validación previa que lanzan `DomainException`.
   * Cero dependencias hacia librerías externas o frameworks de persistencia.

2. **`GroceryApp.Application`**:
   * Orquestación de casos de uso mediante servicios: `PedidoService`, `EntregaService`, `CatalogoAdminService`, `CategoriaService`, `SucursalService`, `DireccionService`, `ZonaResolverService`, `ClienteAuthService`, `EmpleadoAuthService`.
   * Abstracciones desacopladas: `IAppDbContext`, `IEstrategiaAsignacion`, `IJwtTokenGenerator`.
   * Implementación de despacho: `EstrategiaAsignacionCargaSimple`.
   * Manejo consistente de resultados y errores a través de `Result` y `Result<T>`.

3. **`GroceryApp.Infrastructure`**:
   * Mapeo objeto-relacional mediante `GroceryAppDbContext` (EF Core 8).
   * Restricciones de integridad referencial: `DeleteBehavior.Restrict` en entidades críticas (pedidos, entregas, clientes, direcciones).
   * Restricción de unicidad estricta en `Entregas.PedidoId UNIQUE`.
   * Migraciones Code-First y `DbSeeder` con geodatos iniciales (polígono WKT de Carazo) y usuarios semilla.
   * Generación y validación de tokens JWT mediante `JwtTokenGenerator`.

4. **`GroceryApp.Api`**:
   * Endpoints REST versionados (`/api/v1/...`) para consumo de clientes y supervisión.
   * Middleware de `ProblemDetails` para respuestas estandarizadas RFC 7807 ante excepciones y errores de validación.
   * Autenticación basada en JWT (`Bearer`) con políticas de autorización por rol (`Cliente`, `EmpleadoSucursal`, `Repartidor`, `Admin`).
   * Rate limiting en endpoints de autenticación para protección de fuerza bruta.

5. **`GroceryApp.Panel`**:
   * Panel web operativo interactivo en Blazor Server (.NET 8 InteractiveServer).
   * Autenticación basada en Cookies HTTP seguras gestionadas por circuito y `CustomAuthenticationStateProvider`.
   * Aislamiento por rol y sucursal (`SucursalId`) respaldado en servicios de aplicación.
   * Módulos completos para administración de catálogo, supervisión de sucursales, cola de pedidos en tiempo real y módulo de entregas del repartidor.

6. **`GroceryApp.Tests`**:
   * Proyecto de pruebas automatizadas (.NET 8) con `xunit` y base de datos relacional en memoria (`Microsoft.EntityFrameworkCore.Sqlite`).
   * Pruebas completas del flujo de despacho automático, aislamiento por sucursal, conteo de cargas activas, reintentos del Pool y prevención de concurrencia.

---

## 3. Autenticación y Autorización

### Backend (API)
* **Mecanismo:** JSON Web Tokens (JWT) firmados mediante clave simétrica (`HMAC-SHA256`).
* **Claims principales:** `NameIdentifier` (ID del usuario), `Role`, `usuario`, `sucursalId` (cuando aplica a empleados de sucursal o repartidores).
* **Consumo:** Destinado a la futura aplicación móvil Flutter de clientes y clientes API.

### Panel Web (Blazor Server)
* **Mecanismo:** Autenticación por Cookies (`GroceryApp.Panel.Auth`) sincronizada con el circuito de Blazor Server mediante `CustomAuthenticationStateProvider`.
* **Redirección automática tras inicio de sesión:**
  * `Admin` → `/admin/catalogo`
  * `EmpleadoSucursal` → `/sucursal/pedidos`
  * `Repartidor` → `/repartidor/entregas`
* **Control de acceso:**
  * Directiva `@attribute [Authorize(Roles = "...")]` en componentes de página.
  * Aislamiento de sucursal: los empleados de sucursal y repartidores operan exclusivamente en su `SucursalId` asignado.
  * El Administrador posee visibilidad global y selectores de sucursal para supervisión.

---

## 4. Funcionalidades del Panel Blazor Implementadas

1. **Login y Seguridad (`/login`):**
   * Validación de credenciales de empleados contra contraseñas hash (PBKDF2 con `PasswordHasher<Empleado>`).
   * Manejo amigable de errores y cierre de sesión seguro (`/auth/logout`).
2. **Dashboard Administrativo (`/admin/dashboard`):**
   * Métricas rápidas de sucursales, productos y pedidos.
3. **Catálogo Administrativo (`/admin/catalogo`):**
   * Listado de productos con filtros por categoría y búsqueda.
   * Creación y edición de productos, categorías y asignación de fotografías reales almacenadas en disco (`/uploads/productos/`).
   * Configuración de precios por sucursal (`ProductoSucursal.Precio`) y disponibilidad de stock (`StockDisponible: true/false`).
4. **Gestión de Sucursales (`/admin/sucursales`):**
   * Supervisión de sucursales activas, direcciones y horarios de apertura/cierre.
5. **Cola de Pedidos de Sucursal (`/sucursal/pedidos`):**
   * Listado en tiempo real de pedidos entrantes con filtros por estado (`Pendiente`, `Confirmado`, `EnPreparacion`, `Listo`, `EnCamino`, `Entregado`, etc.).
   * Visualización del repartidor asignado a cada pedido listo/en camino.
   * Acciones rápidas de avance de estado: Aceptar/Confirmar, Rechazar, Iniciar Preparación y Marcar como Listo.
6. **Modal de Detalle de Pedido (`DetallePedidoModal.razor`):**
   * Desglose completo de productos, cantidades, precios congelados y totales.
   * Gestión de faltantes durante preparación: eliminación de ítem con recálculo automático de subtotal y total.
   * Marcar como listo disparando la asignación automática.
   * Asignación manual de respaldo para administradores o supervisores.
7. **Módulo de Despacho y Entregas (`/repartidor/entregas`):**
   * Vista de entregas activas exclusivas del repartidor autenticado (`Asignado`, `EnCamino`).
   * Acciones del repartidor: "Iniciar Entrega (En Camino)", "✔ Marcar Entregado (Cobrado)", y "✖ No Entregado (Cliente Ausente / No-Show)".
   * Supervisión de Pool para administradores: visualización en modo solo lectura de pedidos en espera de repartidor.

---

## 5. Modelo de Despacho y Asignación Automática

El modelo de despacho ha sido implementado y verificado conforme a los lineamientos de `AGENTS.md`:

```text
Pedido pasa a "Listo"
        ↓
Proceso automático de asignación
(repartidor de la misma sucursal con menor carga activa en Asignado/EnCamino)
        ↓
   ┌────────────────────────────┴────────────────────────────┐
   ▼                                                         ▼
Repartidor disponible asignado                   No hay repartidor disponible
   ↓                                                         ↓
Mis Entregas (Asignado)                          Queda en "Listo" (Pool de pendientes)
   ↓                                                         ↓
EnCamino                                         Reintento automático cuando un
   ↓                                             repartidor finaliza una entrega
Entregado / NoEntregado
```

### Reglas Operativas Implementadas
1. **Disparo automático:** Al invocar `PedidoService.MarcarListoAsync`, el pedido persiste su estado `Listo` e inmediatamente ejecuta `EntregaService.AsignarAutomaticoAsync(pedidoId)`.
2. **Abstracción desacoplada:** La lógica reside en `IEstrategiaAsignacion`, permitiendo sustituir el algoritmo sin alterar el dominio ni la UI.
3. **Estrategia actual (`EstrategiaAsignacionCargaSimple`):**
   * Considera únicamente repartidores de la sucursal del pedido (`e.Rol == RolEmpleado.Repartidor && e.SucursalId == sucursalId`).
   * Calcula la carga activa contando entregas en estado `Asignado` o `EnCamino`.
   * Entregas en estado `Entregado` o `NoEntregado` no suman carga activa.
   * Selecciona al repartidor con menor carga activa.
   * Criterio de desempate: menor `Id` de empleado (`OrderBy(CargaActiva).ThenBy(Id)`).
4. **Comportamiento sin repartidor (Pool):** Si no existen repartidores elegibles en la sucursal, no se crea `Entrega` y el pedido permanece en `Listo` (definición formal de Pool).
5. **Reintento de despacho:** Al marcar una entrega como `Entregado` o `NoEntregado`, el repartidor se libera y se invoca `ProcesarPendientesPoolAsync(sucursalId)`, asignando en orden de llegada los pedidos pendientes a los repartidores disponibles.
6. **Eliminación de la toma manual:** El repartidor no tiene permitido tomar pedidos del Pool por iniciativa propia; todos los pedidos le son asignados por el sistema.
7. **Alcance deliberado del MVP (No implementado aún):**
   * GPS en tiempo real.
   * Cálculo de distancias y optimización de rutas.
   * Integración con Google Maps u otros servicios externos.
   * Inteligencia artificial o colas externas de dispatch (Hangfire, Redis, RabbitMQ).

---

## 6. Concurrencia e Integridad de Datos

* **Restricción UNIQUE:** Se preserva la configuración `modelBuilder.Entity<Entrega>().HasIndex(e => e.PedidoId).IsUnique();` en base de datos.
* **Protección ante carreras:** En `AsignarAutomaticoAsync` y `AsignarManualAsync`, cualquier intento concurrente de asignar un mismo pedido es capturado mediante `catch (DbUpdateException)`, devolviendo un resultado controlado de fallo sin provocar caídas del sistema.

---

## 7. Pruebas Automatizadas

Se configuró el proyecto `GroceryApp.Tests` en .NET 8 con xUnit y SQLite in-memory:

| # | Prueba | Escenario Validado | Resultado |
|---|---|---|---|
| 1 | `Test1_PedidoListo_ConRepartidorDisponible_SeAsignaAutomaticamente` | Transición a `Listo` asigna entrega automáticamente | Superado |
| 2 | `Test2_DosRepartidores_SeleccionaElDeMenorCargaActiva` | Elección de repartidor con 0 vs 1 entrega activa | Superado |
| 3 | `Test3_RepartidoresConDiferenteNumeroDeEntregasActivas` | Balanceo con 1 vs 2 entregas activas (`Asignado`, `EnCamino`) | Superado |
| 4 | `Test4_SoloSeConsideranRepartidoresDeLaMismaSucursal` | Aislamiento de sucursales (no asigna a otra sucursal) | Superado |
| 5 | `Test5_SinRepartidoresDisponibles_PedidoQuedaListoEnPoolSinEntrega` | Pedido queda en `Listo` sin entrega en el Pool | Superado |
| 6 | `Test6_PedidoPendienteEnPool_SeAsignaCuandoRepartidorQuedaDisponible` | Reintento del Pool al completarse una entrega previa | Superado |
| 7 | `Test7_EntregasEntregadoYNoEntregado_NoCuentanComoCargaActiva` | Históricos `Entregado`/`NoEntregado` no suman carga | Superado |
| 8 | `Test8_NoPermitirDobleEntregaParaElMismoPedido` | Idempotencia y restricción `UNIQUE` en base de datos | Superado |
| 9 | `Test9_EstrategiaPuedeSerSustituida_MedianteIEstrategiaAsignacion` | Desacoplamiento e inyección de estrategias alternativas | Superado |
| 10 | `AsignacionManual_RepartidorDeOtraSucursal_DebeFallar` | Rechazo de asignación manual cruzada entre sucursales y confirmación en misma sucursal | Superado |

**Métricas:** 10 pruebas ejecutadas, 10 exitosas, 0 fallidas (100% de éxito).

---

## 8. Sprint 4.1 Hardening

- **Validación de sucursal en asignación manual:** `EntregaService.AsignarManualAsync` valida que el repartidor exista, tenga rol `Repartidor` y pertenezca obligatoriamente a la misma sucursal del pedido (`pedido.SucursalId == repartidor.SucursalId`). Cualquier intento de cruzar sucursales resulta en fallo sin crear registros de `Entrega`.
- **Protección de autorización:** `PanelPedidosController` audita y restringe el ámbito de sucursales (`SucursalScope`), protegiendo la asignación manual exclusivamente para `Admin` y `EmpleadoSucursal` (dentro de su sucursal). `Repartidor` y `Cliente` no tienen acceso.
- **Prueba automatizada de asignación cruzada:** Test específico implementado y superado en `GroceryApp.Tests/DespachoAutomaticoTests.cs`.
- **Build/tests verificados:** Solución compilada con 0 advertencias y 0 errores; 10 pruebas xUnit ejecutadas y superadas. Ausencia confirmada de regresiones de autoservicio (`TomarAsync`, `ListarDisponiblesAsync`, `/tomar`, `/disponibles`).

---

## 9. Base de Datos y Migraciones

Migraciones Code-First aplicadas:
1. `20260725212427_InitialCreate`: Estructura inicial completa (tablas, relaciones, restricciones de chequeo e índices únicos).
2. `20260726171708_AgregarPoligonoAZona`: Incorporación de columna `PoligonoWkt` en tabla `Zonas` para delimitación geográfica.

No se requieren migraciones adicionales para el modelo de despacho de Sprint 4 ni Sprint 4.1.

---

## 10. Deuda Técnica Real y Conocida

1. **Almacenamiento local de fotografías de productos:** Las imágenes subidas desde el panel se guardan en el sistema de archivos local (`wwwroot/uploads/productos/`). Para un despliegue en múltiples instancias o nube se requerirá migrar a Blob Storage (ej. Azure Blob Storage o AWS S3).
2. **Evaluación de polígonos geoespaciales en memoria:** `ZonaResolverService` evalúa la cobertura utilizando NetTopologySuite en memoria en lugar de utilizar tipos espaciales nativos de SQL Server (`geography`), adecuado para el casco urbano inicial pero optimizable a futuro.
3. **Semillas de prueba con credenciales estáticas:** `DbSeeder` incluye usuarios semilla de prueba (`admin`, `operador`, `repartidor1`). Se debe forzar el cambio de credenciales antes del pase a producción.
4. **Decisiones de negocio pendientes:** 
   * Definición del monto exacto de envío para "Municipios aledaños".
   * Política automática definitiva ante clientes con acumulación reiterada de no-shows.

---

## 11. Próximos Pasos Recomendados

1. **Autorizar y consolidar el commit de cierre de Sprint 4 / Sprint 4.1.**
2. **Avanzar al Sprint 5:**
   * Crear el proyecto Flutter para clientes (`grocery_app_mobile`).
   * Configurar arquitectura base móvil (gestor de estado, cliente HTTP con Dio, almacenamiento seguro de token).
   * Implementar autenticación del cliente (registro y login contra API) y visualización del catálogo público y categorías.
