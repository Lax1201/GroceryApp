# Hoja de Ruta — Plataforma de Pedidos de Abarrotes

Cadena propia de tiendas (no SaaS). App Android (Flutter) + Panel web (Blazor) + Backend (ASP.NET Core Web API, SQL Server, EF Core, JWT). Desarrollo en solitario.

---

## Fase 0 — Descubrimiento

**Visión**: que una familia nicaragüense haga su mandado de abarrotes desde el celular, con precios y variedad de mercado tradicional, entregado a domicilio.

**Alcance del MVP**: lanzamiento en el casco urbano de Carazo, arrancando con **una sola sucursal**. Sistema listo para escalar a más sucursales/ciudades después.

**Propuesta de valor**
- Precios de mercado tradicional, no premium.
- Pago contra entrega — sin fricción de adopción digital.
- Catálogo amplio orientado a canasta básica.

**Ventajas competitivas**
- Cadena propia con sucursales físicas reales (no depende de repartidores de terceros).
- Control total de precio/inventario por sucursal → competitividad local barrio por barrio.

**Riesgo de negocio identificado**: pago 100% contra entrega implica riesgo de pedidos no-show ya preparados. Resuelto en reglas de negocio (Fase 1).

**Criterio de éxito del MVP**: alcanzar cierto número de pedidos completados por semana (meta numérica a definir con datos reales de la operación).

---

## Fase 1 — Reglas del negocio

**Estados del pedido**

```
Pendiente → Confirmado → En preparación → Listo para entrega → En camino → Entregado
                ↓                                                    ↓
            Rechazado                                            No entregado
Cancelado (solo antes de "En preparación")
```

- Pedido, pago y entrega se manejan como procesos independientes.

**Reglas definidas**

| Tema | Regla |
|---|---|
| Producto agotado en preparación | Se elimina del pedido y se ajusta el total automáticamente. Sin sustitución ni contacto al cliente. |
| Costo de envío | Tarifa fija única dentro del casco urbano de Carazo. Municipios aledaños: cuota adicional (monto por definir). |
| No-show (cliente ausente) | Se marca la cuenta del cliente; tras varios no-shows pasa a revisión. |
| Cobertura | Casco urbano de Carazo (zona base) + municipios aledaños (zona extendida, con cuota a definir). |
| Horario fuera de atención | El pedido se bloquea, no se acepta. |
| Monto mínimo de pedido | No hay monto mínimo. |

---

## Fase 2 — Diseño funcional

**Roles del sistema**

| Rol | Acceso | Funciones |
|---|---|---|
| Cliente | App Android | Comprar, ver historial, seguir pedido |
| Empleado de sucursal | Panel web | Ver/aceptar/cancelar pedidos, cambiar estado, marcar listo |
| Repartidor | Panel web (vista simplificada, rol separado) | Ver pedidos asignados, marcar en camino / entregado / no entregado |
| Admin | Panel web | Gestión de catálogo (productos, precios, stock) + funciones de empleado de sucursal |

**Funcionalidades del MVP**
- *Cliente*: registro, login, recuperar contraseña, direcciones, catálogo, categorías, búsqueda, carrito, checkout, pedido, historial, seguimiento.
- *Operación*: cola de pedidos entrantes, aceptar/rechazar, cambiar estado, marcar listo.
- *Entrega*: vista de repartidor, marcar en camino/entregado/no entregado, contador de no-shows por cliente.
- *Catálogo*: alta/edición de producto (nombre, precio, categoría, foto, stock disponible sí/no).

**Fuera del MVP**: ERP, POS, compras, proveedores, contabilidad, facturación, transferencias entre sucursales, inventario avanzado, pagos digitales, app propia de repartidor, tarifa de zona extendida con precio definitivo.

**Formato de historia de usuario**

> **Como** cliente, **quiero** ver el estado de mi pedido en tiempo real, **para** saber cuándo va a llegar.
> **Criterios de aceptación**:
> - El estado visible coincide exactamente con el estado interno del pedido.
> - Si el pedido pasa a "No entregado", el cliente ve un mensaje claro, no un estado técnico.
> - El estado se actualiza sin que el cliente tenga que refrescar manualmente.

---

## Fase 3 — UX

**Flujo del cliente**: Login/registro → Home (catálogo/categorías) → buscar o navegar → detalle de producto → carrito → checkout (dirección con pin + referencia → tarifa de envío → confirmar) → pedido creado → seguimiento en vivo → historial.

**Mapa de navegación — App Android**
```
Splash
 └─ Login ──┬─ Registro
             └─ Recuperar contraseña
Home
 ├─ Categoría → Lista de productos → Detalle de producto
 ├─ Búsqueda → Resultados → Detalle de producto
 ├─ Carrito → Checkout → Confirmación de pedido → Seguimiento
 ├─ Historial de pedidos → Detalle de pedido pasado
 └─ Perfil → Direcciones (lista/agregar/editar con mapa) → Cerrar sesión
```

**Pantallas — Panel Web**
- Login (redirige según rol)
- Admin: Catálogo (lista) → Crear/Editar producto
- Empleado de sucursal: Cola de pedidos → Detalle → cambiar estado / marcar listo
- Repartidor: Pedidos asignados → Detalle → marcar en camino / entregado / no entregado

**Captura de ubicación**: pin en mapa (GPS) + referencia en texto libre. El sistema calcula la zona (casco urbano / municipio aledaño) automáticamente al guardar el pin.

**Componentes reutilizables**: tarjeta de producto, badge de estado de pedido (color por estado), selector de cantidad, tarjeta de dirección, barra de búsqueda, mapa con pin seleccionable, bottom nav.

**Validaciones clave**
- Registro: teléfono válido y único, contraseña mínima.
- Dirección: pin obligatorio, referencia en texto obligatoria.
- Checkout: carrito no vacío, dirección seleccionada, dentro de horario, tarifa de envío visible antes de confirmar.
- Catálogo (admin): precio > 0, producto sin categoría no se puede publicar.

---

## Fase 4 — Arquitectura

**Arquitectura general**: backend único ASP.NET Core Web API en capas simples (sin microservicios ni CQRS/DDD complejo — sobra para este tamaño de MVP).

```
API (Controllers)
 └─ Application (servicios/casos de uso)
     └─ Domain (entidades, reglas de negocio)
         └─ Infrastructure (EF Core, SQL Server, repositorios)
```

Una sola API sirve a la app Android y al panel web. Los roles (Cliente/EmpleadoSucursal/Repartidor/Admin) se resuelven vía claims en el JWT.

**Panel web**: Blazor **Server** (no WebAssembly) — más liviano para pocos usuarios internos, puede invocar la capa de Application directamente, menor consumo de recursos en un VPS económico.

**Entidades principales del dominio**
- `Cliente`, `Direccion` (lat/long + referencia + zona), `Zona`, `Sucursal`, `Empleado` (con rol), `Categoria`, `Producto`, `ProductoSucursal` (precio/stock por sucursal), `Pedido`, `PedidoItem`, `Entrega`.

**API REST — recursos principales**
```
POST   /auth/login
POST   /auth/registro
POST   /auth/recuperar-password

GET    /catalogo/categorias
GET    /catalogo/productos?categoria=&busqueda=
GET    /catalogo/productos/{id}

GET/POST/PUT /direcciones

POST   /pedidos
GET    /pedidos/{id}
GET    /pedidos/historial
GET    /pedidos/{id}/seguimiento

GET    /panel/pedidos?estado=
PUT    /panel/pedidos/{id}/estado

GET    /panel/entregas
PUT    /panel/entregas/{id}/estado

GET/POST/PUT /panel/productos
```

**Seguridad**: JWT + HTTPS obligatorio (Let's Encrypt, gratuito). Rate limiting en `/auth/login`.

**Organización de la solución .NET**
```
GroceryApp.Api            → controllers, JWT config
GroceryApp.Panel          → Blazor Server (panel admin/empleados/repartidor)
GroceryApp.Application    → casos de uso / servicios
GroceryApp.Domain         → entidades, reglas de negocio
GroceryApp.Infrastructure → EF Core, DbContext, repositorios
GroceryApp.Shared         → DTOs compartidos entre Api y Panel
```

**Hosting**: VPS propio económico (Linux o Windows a definir en despliegue).

---

## Fase 5 — Base de datos

```sql
Clientes
  Id PK, Nombre, Telefono UNIQUE, Email, PasswordHash,
  FechaRegistro, NoShowCount INT DEFAULT 0

Direcciones
  Id PK, ClienteId FK→Clientes, Latitud, Longitud,
  Referencia, ZonaId FK→Zonas, EsPrincipal BIT

Zonas
  Id PK, Nombre, Tipo (CascoUrbano | MunicipioAledano),
  TarifaEnvio DECIMAL, Activa BIT

Sucursales
  Id PK, Nombre, Direccion, HorarioApertura, HorarioCierre

Empleados
  Id PK, Nombre, Usuario UNIQUE, PasswordHash,
  Rol (EmpleadoSucursal | Repartidor | Admin),
  SucursalId FK→Sucursales NULL (NULL solo si Rol=Admin)

Categorias
  Id PK, Nombre

Productos
  Id PK, Nombre, Descripcion, CategoriaId FK→Categorias, FotoUrl

ProductosSucursal
  Id PK, ProductoId FK→Productos, SucursalId FK→Sucursales,
  Precio DECIMAL CHECK(Precio > 0), StockDisponible BIT,
  UNIQUE(ProductoId, SucursalId)

Pedidos
  Id PK, ClienteId FK→Clientes, SucursalId FK→Sucursales,
  DireccionId FK→Direcciones,
  Estado (Pendiente|Confirmado|EnPreparacion|Listo|EnCamino|Entregado|NoEntregado|Cancelado|Rechazado),
  TarifaEnvio DECIMAL, Subtotal DECIMAL, Total DECIMAL,
  FechaCreacion, FechaActualizacion

PedidoItems
  Id PK, PedidoId FK→Pedidos, ProductoId FK→Productos,
  Cantidad INT CHECK(Cantidad > 0), PrecioUnitario DECIMAL, Subtotal DECIMAL

Entregas
  Id PK, PedidoId FK→Pedidos UNIQUE, RepartidorId FK→Empleados,
  Estado (Asignado|EnCamino|Entregado|NoEntregado),
  FechaAsignacion, FechaEntrega
```

**Índices clave**: `Clientes.Telefono` (único), `ProductosSucursal(ProductoId, SucursalId)` (único), `Pedidos(SucursalId, Estado)`, `Pedidos.ClienteId`.

**Regla de integridad**: `PedidoItems.PrecioUnitario` se guarda al momento de la compra; los pedidos históricos no cambian si el precio del producto cambia después.

**Migraciones**: EF Core Code-First, una migración por feature/sprint, nunca editar una migración ya aplicada en producción. Seed inicial de `Zonas` y `Categorias` en Sprint 0.

---

## Fase 6 — Backlog

| Épica | Historias clave | Prioridad | Depende de |
|---|---|---|---|
| E1 — Autenticación (cliente) | Registro, login, recuperar contraseña | P0 | — |
| E2 — Direcciones y zonas | Agregar dirección con pin+referencia, cálculo automático de zona | P0 | E1 |
| E3 — Catálogo admin | Crear/editar productos, precio y stock por sucursal | P0 | — |
| E4 — Catálogo cliente | Ver categorías, buscar productos | P0 | E1, E3 |
| E5 — Carrito y checkout | Carrito, confirmar pedido con tarifa y total, bloqueo fuera de horario | P0 | E2, E3, E4 |
| E6 — Operación de pedidos | Cola de pedidos, aceptar/rechazar, cambiar estado, ajuste por producto faltante | P0 | E5 |
| E7 — Entregas | Ver asignados, marcar en camino/entregado/no entregado, contador no-shows | P0 | E6 |
| E8 — Seguimiento e historial | Estado en vivo, historial de pedidos | P1 | E6, E7 |

```
E1 → E2 ↘
E1 → E3 → E4 → E5 → E6 → E7 → E8
```

---

## Fase 7 — Planificación Scrum (desarrollo en solitario)

Orden pensado para minimizar cambios de stack: primero todo el backend, luego el panel (mismo stack C#), al final toda la app Flutter.

| Sprint | Foco | Entregable |
|---|---|---|
| 0 | Fundación técnica: solución .NET, SQL Server + migración inicial, seed de Zonas/Categorías, JWT, VPS con HTTPS | API responde en producción, vacía de lógica de negocio |
| 1 | Backend: Autenticación (E1) | Endpoints de auth con roles, probados en Swagger/Postman |
| 2 | Backend: Catálogo y Zonas (E2 + E3) | Endpoints de productos, precio/stock por sucursal, cálculo de zona |
| 3 | Backend: Pedidos y Entregas (E5 + E6 + E7) | Backend 100% funcional y probado por Postman, sin UI |
| 4 | Panel Blazor completo | Login por rol, catálogo, cola de pedidos, vista de entregas — panel operativo 100% usable |
| 5 | Flutter: base + catálogo cliente (E1 + E4) | Login/registro, navegación, categorías, búsqueda, detalle de producto |
| 6 | Flutter: direcciones + carrito + checkout (E2 + E5) | Mapa con pin, carrito, checkout con tarifa y validación de horario |
| 7 | Flutter: seguimiento + historial + perfil (E8) | Estado en vivo, historial, gestión de direcciones |
| 8 | Integración real + hardening | Prueba end-to-end real, rate limiting, corrección de bugs, MVP listo para clientes reales |

**Definition of Done (toda historia, todo sprint)**
- Cumple todos sus criterios de aceptación.
- Probado en dispositivo Android real (no solo emulador) y en el panel desplegado.
- Sin errores de consola/logs en el flujo completo de esa historia.
- Migraciones EF Core aplicadas sin romper datos existentes.
- Revisión de código propia antes de dar por cerrada la historia.

---

## Pendientes a resolver más adelante (no bloquean el MVP)
- Monto exacto de la cuota de envío para municipios aledaños a Carazo.
- Qué significa exactamente "cuenta en revisión" tras varios no-shows (bloqueo, contacto manual, etc.).
- Meta numérica concreta de pedidos/semana para el criterio de éxito.
- Rentabilidad de la tarifa fija de envío en pedidos lejanos y de bajo monto — revisar con datos reales de operación.
