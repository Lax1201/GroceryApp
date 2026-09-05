# AGENTS.md

# GroceryApp — Instrucciones para Agentes de IA

## 1. Propósito de este archivo

Este archivo contiene las reglas operativas que deben seguir los agentes de IA que trabajen en el repositorio **GroceryApp**.

Su objetivo es preservar la arquitectura, las decisiones de negocio y el trabajo previamente realizado, evitando cambios innecesarios, duplicación de lógica, regresiones o desviaciones del alcance definido.

Este archivo **no sustituye la Hoja de Ruta Oficial**.

La Hoja de Ruta Oficial es la fuente de verdad respecto a:

- visión del producto;
- alcance del MVP;
- reglas de negocio;
- arquitectura;
- entidades;
- API;
- backlog;
- sprints;
- Definition of Done.

Archivo de referencia:

`hoja-de-ruta-app-abarrotes.md`

Si existe una contradicción entre una implementación existente y la Hoja de Ruta Oficial, el agente debe **identificar la contradicción antes de modificar código**.

---

# 2. Identidad del proyecto

**GroceryApp** es una plataforma propia de pedidos de abarrotes a domicilio.

No es un SaaS.

El MVP está orientado inicialmente al casco urbano de Carazo, Nicaragua, comenzando con una sola sucursal y diseñado para poder escalar posteriormente a más sucursales y ciudades.

La plataforma está compuesta por:

- Aplicación Android para clientes: Flutter.
- Panel web para operación interna: Blazor Server.
- Backend: ASP.NET Core Web API.
- Base de datos: SQL Server.
- ORM: Entity Framework Core.
- Autenticación API: JWT.
- Autenticación del Panel: cookies/circuito de Blazor Server.
- Comunicación segura: HTTPS.

---

# 3. Fuente de verdad y prioridad de instrucciones

Cuando el agente tome decisiones debe respetar el siguiente orden:

1. Requisitos explícitos de la tarea actual.
2. `AGENTS.md`.
3. `hoja-de-ruta-app-abarrotes.md`.
4. Arquitectura y reglas de negocio existentes en el código.
5. Convenciones existentes del repositorio.

El agente **no debe inventar requisitos**.

Cuando una decisión no esté definida:

1. revisar el código existente;
2. revisar la hoja de ruta;
3. buscar una solución coherente con la arquitectura actual;
4. elegir la solución mínima necesaria;
5. documentar la decisión si tiene impacto arquitectónico o de negocio.

---

# 4. Estado actual del proyecto

Estado al cierre de Sprint 4:

- Sprint 0: completado.
- Sprint 1: completado.
- Sprint 2: completado.
- Sprint 3: completado.
- Sprint 4: completado.
- Sprints 5–8: pendientes.

El backend correspondiente a los Sprints 0–3 se considera funcional y debe tratarse como código existente que debe preservarse.

Esto NO significa que el backend sea intocable.

Puede modificarse cuando exista una razón legítima, por ejemplo:

- corrección de bugs;
- vulnerabilidades;
- errores de integración;
- incompatibilidades;
- requisitos explícitos del sprint actual;
- mejoras necesarias para cumplir los criterios de aceptación.

No realizar modificaciones de backend únicamente para "refactorizar", "modernizar" o "mejorar" código sin una necesidad concreta.

---

# 5. Objetivo actual

El proyecto se encuentra en:

## Sprint 4 — Panel Web Blazor Server

El objetivo es construir un panel operativo funcional para:

- Admin;
- EmpleadoSucursal;
- Repartidor.

El cliente NO utiliza el Panel.

El cliente utilizará posteriormente la aplicación Flutter.

El entregable del Sprint 4 es:

> Login por rol, catálogo, cola de pedidos y vista de entregas — panel operativo 100% usable.

---

# 6. Arquitectura obligatoria

La arquitectura general es:

```text
Flutter
   │
   ▼
ASP.NET Core Web API
   │
   ├── Application
   ├── Domain
   └── Infrastructure
          │
          ▼
      SQL Server


Blazor Server
   │
   ▼
Application
   │
   ├── Domain
   └── Infrastructure
          │
          ▼
      SQL Server
```

La solución debe mantener:

```text
GroceryApp.Api
GroceryApp.Panel
GroceryApp.Application
GroceryApp.Domain
GroceryApp.Infrastructure
GroceryApp.Shared
```

Si `GroceryApp.Shared` todavía no existe, **no crearlo únicamente por cumplir nominalmente la estructura del roadmap**.

Los DTO existentes actualmente en:

```text
GroceryApp.Application/Dtos/
```

deben mantenerse allí salvo que exista una necesidad técnica concreta para moverlos.

---

# 7. Regla fundamental de separación de responsabilidades

La UI no debe contener lógica de negocio.

No hacer:

```text
Componente Blazor
    ↓
DbContext
    ↓
SQL Server
```

Preferir:

```text
Componente Blazor
    ↓
Application Service
    ↓
Domain
    ↓
Infrastructure / EF Core
    ↓
SQL Server
```

El Panel debe reutilizar los servicios existentes siempre que sea posible.

Antes de crear un nuevo servicio:

1. buscar si ya existe uno equivalente;
2. revisar sus métodos;
3. reutilizarlo si cubre la operación;
4. ampliar el servicio existente si corresponde;
5. crear uno nuevo únicamente cuando exista una responsabilidad realmente diferente.

---

# 8. Regla contra duplicación de lógica

La lógica de negocio debe existir en un único lugar.

No duplicar en:

- Razor;
- controllers;
- servicios;
- JavaScript;
- Flutter;
- consultas SQL independientes.

Ejemplos:

La transición de estados de un pedido debe estar protegida por el dominio.

El cálculo del total debe pertenecer a la lógica de negocio correspondiente.

La resolución de zona debe utilizar `ZonaResolverService`.

La autorización por sucursal debe respetar el `SucursalId` del usuario.

La UI solamente debe presentar y solicitar operaciones.

---

# 9. Máquina de estados del pedido

Estados válidos:

```text
Pendiente
Confirmado
EnPreparacion
Listo
EnCamino
Entregado
NoEntregado
Cancelado
Rechazado
```

Flujo principal:

```text
Pendiente
   ↓
Confirmado
   ↓
EnPreparacion
   ↓
Listo
   ↓
EnCamino
   ↓
Entregado
```

Flujos alternativos:

```text
Pendiente → Rechazado

Pendiente → Cancelado
```

`Cancelado` solamente es válido antes de `EnPreparacion`.

Durante preparación puede ocurrir:

```text
EnPreparacion
    ↓
producto faltante
    ↓
eliminar PedidoItem
    ↓
recalcular subtotal
    ↓
recalcular total
```

El Panel nunca debe modificar directamente el campo `Estado` en la base de datos.

Debe utilizar los métodos y servicios de dominio existentes.

---

# 10. Máquina de estados de Entrega

Estados:

```text
Asignado
EnCamino
Entregado
NoEntregado
```

Los cambios deben utilizar `EntregaService` y la lógica existente del dominio.

No modificar directamente el estado mediante EF Core desde la UI.

---

# 11. Reglas de precios

El precio utilizado por un pedido debe congelarse en:

```text
PedidoItem.PrecioUnitario
```

Cuando se crea el pedido:

```text
ProductoSucursal.Precio
        ↓
PedidoItem.PrecioUnitario
```

Una modificación posterior del precio del producto NO debe modificar pedidos históricos.

Nunca recalcular pedidos históricos utilizando el precio actual del catálogo.

---

# 12. Reglas de sucursales

Los productos tienen configuración por sucursal mediante:

```text
ProductoSucursal
```

El precio y disponibilidad pueden variar por sucursal.

Un empleado con:

```text
Rol = EmpleadoSucursal
```

solo puede operar sobre su:

```text
SucursalId
```

El Admin tiene visibilidad global.

El Repartidor opera únicamente dentro del ámbito de su sucursal.

Nunca confiar únicamente en filtros de la UI para proteger el acceso por sucursal.

La autorización debe estar respaldada por la capa de aplicación/backend.

---

# 13. Roles

Roles válidos:

```text
Cliente
EmpleadoSucursal
Repartidor
Admin
```

## Cliente

Opera exclusivamente mediante:

```text
API + aplicación Flutter
```

No tiene acceso al Panel.

## EmpleadoSucursal

Puede:

- ver pedidos de su sucursal;
- aceptar pedidos;
- rechazar pedidos;
- iniciar preparación;
- marcar pedidos como listos;
- gestionar productos según los permisos definidos.

## Repartidor

Puede:

- visualizar sus entregas asignadas;
- marcar EnCamino;
- marcar Entregado;
- marcar NoEntregado.

El repartidor NO selecciona ni toma pedidos manualmente; las entregas le son asignadas automáticamente por el sistema según la estrategia de despacho configurada o mediante asignación manual de respaldo de la sucursal.

## Admin

Tiene visibilidad global y puede:

- gestionar catálogo;
- gestionar categorías;
- gestionar sucursales;
- gestionar empleados;
- visualizar/operar pedidos globalmente según las reglas definidas.

---

# 14. Autenticación del Panel

El Panel es:

```text
Blazor Server
```

No convertirlo a Blazor WebAssembly.

El login del Panel corresponde a empleados.

Debe utilizar autenticación mediante cookies/circuito de Blazor Server y mantener los claims necesarios:

```text
Role
NameIdentifier
sucursalId
```

Después del login:

```text
Admin
    → área administrativa

EmpleadoSucursal
    → cola de pedidos de su sucursal

Repartidor
    → vista de entregas
```

La navegación debe utilizar autorización basada en roles.

---

# 15. Sprint 4 — Componentes esperados

Estructura aproximada:

```text
GroceryApp.Panel
│
├── Components
│   ├── Pages
│   │   ├── Login.razor
│   │   ├── Admin
│   │   │   ├── Catalogo.razor
│   │   │   ├── EditarProducto.razor
│   │   │   └── Sucursales.razor
│   │   │
│   │   ├── Sucursal
│   │   │   ├── ColaPedidos.razor
│   │   │   └── DetallePedidoModal.razor
│   │   │
│   │   └── Repartidor
│   │       ├── EntregasPool.razor
│   │       └── MisEntregas.razor
│   │
│   └── Shared
│       └── componentes reutilizables
│
└── Auth
    └── CustomAuthenticationStateProvider.cs
```

Esta estructura es orientativa.

No crear archivos innecesarios únicamente para coincidir literalmente con ella.

---

# 16. Funcionalidad del catálogo administrativo

Debe permitir:

- listar productos;
- crear productos;
- editar productos;
- administrar categorías;
- seleccionar sucursal;
- modificar precio;
- modificar disponibilidad;
- subir fotografías.

Reglas:

```text
Precio > 0
Producto debe tener categoría para publicarse
```

El stock del MVP se representa como disponibilidad:

```text
StockDisponible = true / false
```

No implementar un sistema de cantidades de inventario a menos que la hoja de ruta sea modificada explícitamente.

---

# 17. Cola de pedidos

El empleado de sucursal debe visualizar automáticamente los pedidos correspondientes a su sucursal.

El Admin puede visualizar pedidos globalmente.

Las operaciones deben utilizar los servicios existentes, especialmente:

```text
PedidoService
```

y sus métodos disponibles.

No duplicar las reglas de transición de estado dentro de los componentes Razor.

---

# 18. Modelo de despacho y asignación de repartidores

El repartidor utiliza el Panel Web.

NO crear una aplicación Flutter independiente para repartidores en el MVP.

### Reglas de despacho y asignación automática

1. **Sin selección manual:** El repartidor **NO** selecciona ni toma pedidos manualmente.
2. **Disparo de asignación:** Cuando un pedido pasa al estado `Listo`, debe entrar al proceso automático de asignación.
3. **Aislamiento por sucursal:** La asignación solo puede considerar repartidores pertenecientes a la misma sucursal (`SucursalId`) del pedido.
4. **Estrategia de asignación para la primera etapa:**
   Para la primera etapa del producto (diseñada para operar aproximadamente un año en una sola ciudad y con una flota inicial de 1–2 repartidores), la estrategia de asignación será deliberadamente simple:
   * Seleccionar automáticamente al repartidor elegible con menor cantidad de entregas activas (`Asignado` o `EnCamino`).
5. **Alcance deliberado del MVP (No implementar todavía):**
   * GPS en tiempo real de repartidores.
   * Cálculo de distancia.
   * Optimización de rutas.
   * Integración con Google Maps u otros servicios de mapas para despacho.
   * Algoritmos complejos de balanceo.
   * Inteligencia artificial para asignación.
   La ausencia de estas capacidades avanzadas en el MVP es intencional y no debe considerarse una deficiencia técnica.
6. **Disponibilidad y concepto de Pool:**
   * Si no existe ningún repartidor elegible disponible, el pedido debe permanecer en estado `Listo` dentro del pool de pedidos pendientes de asignación.
   * Cuando posteriormente exista un repartidor disponible (por ejemplo, al finalizar una entrega o incorporarse a la sucursal), el sistema deberá poder procesar esos pedidos pendientes y asignarlos automáticamente.
   * **Definición formal de Pool:** El concepto de "pool" queda definido como pedidos en estado `Listo` que todavía no tienen un repartidor asignado. **NO significa que los repartidores puedan seleccionar libremente un pedido.**
7. **Desacoplamiento arquitectónico y extensibilidad:**
   * La estrategia de asignación debe diseñarse y desacoplarse de forma que pueda reemplazarse posteriormente sin reescribir la lógica principal de pedidos, entregas, API, Panel o aplicación móvil.
   * La arquitectura debe permitir que en el futuro se agreguen estrategias más avanzadas (proximidad geográfica, ubicación actual, carga, tiempo estimado, optimización de rutas, múltiples sucursales y ciudades).

### Principio de escalabilidad

> "El sistema debe ser simple para la escala inicial, pero no debe quedar acoplado a la estrategia inicial de asignación. La primera implementación prioriza simplicidad y eficiencia operacional para una ciudad con una flota pequeña; las futuras estrategias de despacho deben poder incorporarse mediante una nueva estrategia de asignación sin alterar el núcleo del dominio."

### Flujo de estados de despacho

```text
Pedido pasa a "Listo"
        ↓
Proceso automático de asignación
(repartidor elegible con menor cantidad de entregas activas en la misma sucursal)
        ↓
   ┌────────────────────────────┴────────────────────────────┐
   ▼                                                         ▼
Repartidor disponible asignado                   No hay repartidor disponible
   ↓                                                         ↓
Mis Entregas (Asignado)                          Queda en "Listo" (Pool de pendientes)
   ↓                                                         ↓
EnCamino                                         Asignación automática posterior
   ↓
Entregado / NoEntregado
```

### Concurrencia e Integridad

La asignación debe conservar la protección de concurrencia existente.

No eliminar ni debilitar la restricción:

```text
Entregas.PedidoId UNIQUE
```

---

# 19. Regla de no-show

Cuando una entrega termina como:

```text
NoEntregado
```

debe conservarse el comportamiento existente de incremento de:

```text
Cliente.NoShowCount
```

No implementar todavía políticas adicionales como:

- bloqueo automático;
- suspensión;
- contacto automático;
- revisión automática;

salvo que sean solicitadas explícitamente.

Estas decisiones están pendientes en la hoja de ruta.

---

# 20. Base de datos y EF Core

Utilizar:

```text
EF Core Code-First
```

Las migraciones deben seguir el patrón existente.

Reglas:

- No editar migraciones ya aplicadas.
- Crear nuevas migraciones cuando haya cambios de esquema.
- No eliminar datos existentes deliberadamente.
- No modificar restricciones existentes sin justificación.
- Respetar claves foráneas.
- Respetar índices únicos.
- Respetar Check Constraints.
- Respetar `DeleteBehavior.Restrict` donde esté configurado.

Antes de crear una migración:

1. revisar el modelo actual;
2. revisar migraciones existentes;
3. comprobar si el cambio realmente requiere modificación del esquema;
4. generar la migración;
5. revisar el contenido generado.

---

# 21. Seguridad

La seguridad no debe depender exclusivamente de la interfaz.

Debe mantenerse:

- JWT en API;
- HTTPS;
- rate limiting en autenticación;
- hash seguro de contraseñas;
- autorización basada en roles;
- aislamiento por `SucursalId`;
- validación server-side;
- validación de entradas;
- protección contra acceso no autorizado.

No almacenar contraseñas en texto plano.

No introducir secretos directamente en código fuente.

No incluir:

- contraseñas;
- tokens;
- claves privadas;
- connection strings sensibles;

en commits.

---

# 22. Cambios de arquitectura

No realizar por iniciativa propia:

- migración de Blazor Server a WebAssembly;
- microservicios;
- CQRS;
- DDD complejo;
- Mediator;
- nuevos sistemas de mensajería;
- Redis;
- Kubernetes;
- nuevos servicios externos;
- nuevos frameworks;
- nuevas bases de datos;

si no existe una necesidad concreta relacionada con el requisito actual.

El objetivo es mantener una arquitectura simple y mantenible para un desarrollo en solitario.

---

# 23. Dependencias

Antes de agregar un paquete NuGet:

1. comprobar si la funcionalidad ya puede resolverse con dependencias existentes;
2. comprobar compatibilidad con .NET utilizado por el proyecto;
3. comprobar si la dependencia es realmente necesaria;
4. evitar dependencias redundantes.

No agregar paquetes únicamente por preferencia personal.

---

# 24. Regla de reutilización

Antes de crear:

- entidad;
- servicio;
- DTO;
- repository;
- helper;
- componente;
- endpoint;

buscar primero en el repositorio.

El código existente es la primera opción.

---

# 25. Protocolo obligatorio antes de modificar código

Antes de implementar una tarea:

### Paso 1 — Comprender

Leer:

```text
AGENTS.md
hoja-de-ruta-app-abarrotes.md
```

### Paso 2 — Inspeccionar

Buscar:

- entidades relacionadas;
- servicios existentes;
- DTOs;
- endpoints;
- componentes;
- configuración;
- pruebas;
- migraciones.

### Paso 3 — Determinar alcance

Identificar:

- qué requiere la tarea;
- qué ya existe;
- qué falta;
- qué no debe modificarse.

### Paso 4 — Diseñar

Elegir la solución mínima compatible con la arquitectura.

### Paso 5 — Implementar

Realizar únicamente los cambios necesarios.

---

# 26. Protocolo después de modificar código

Después de implementar:

1. compilar la solución;
2. ejecutar pruebas disponibles;
3. verificar errores de compilación;
4. revisar warnings relevantes;
5. revisar logs;
6. comprobar rutas y navegación;
7. verificar autorización;
8. comprobar que no se rompieron funcionalidades existentes;
9. revisar los cambios realizados;
10. verificar criterios de aceptación.

No considerar una tarea terminada únicamente porque el código compila.

---

# 27. Definition of Done

Una historia se considera terminada únicamente cuando:

- cumple todos sus criterios de aceptación;
- funciona con el código existente;
- no introduce regresiones;
- no presenta errores de consola/logs en el flujo probado;
- las migraciones EF Core son correctas;
- la autorización es correcta;
- las reglas de negocio se respetan;
- el código fue revisado.

Para funcionalidades móviles:

- debe probarse en dispositivo Android real cuando corresponda.

Para funcionalidades del Panel:

- debe probarse en el Panel desplegado cuando el entorno esté disponible.

---

# 28. Qué NO debe hacer el agente

No:

- reescribir código funcional sin necesidad;
- cambiar el stack;
- crear microservicios;
- duplicar servicios;
- duplicar reglas de negocio;
- modificar estados directamente en la base de datos desde la UI;
- ignorar autorización por sucursal;
- crear una app independiente para repartidores;
- implementar funcionalidades fuera del MVP sin autorización;
- eliminar funcionalidades existentes para simplificar;
- eliminar migraciones existentes;
- editar migraciones ya aplicadas;
- introducir secretos;
- cambiar decisiones de negocio por iniciativa propia.

---

# 29. Alcance fuera del MVP

No implementar actualmente:

- ERP;
- POS;
- compras;
- proveedores;
- contabilidad;
- facturación;
- transferencias entre sucursales;
- inventario avanzado;
- pagos digitales;
- aplicación nativa de repartidor;
- tarifa definitiva para municipios aledaños;
- política automática definitiva contra no-shows.

Estas funcionalidades pueden considerarse posteriormente.

---

# 30. Pendientes de negocio

Existen decisiones todavía pendientes:

- monto exacto de envío para municipios aledaños;
- comportamiento exacto después de varios no-shows;
- meta numérica de pedidos por semana;
- rentabilidad de tarifas según distancia y monto.

No inventar valores definitivos para estas decisiones.

Si una funcionalidad depende de una decisión pendiente, utilizar una solución provisional explícita o detenerse y señalar la dependencia.

---

# 31. Estado de los Sprints

```text
Sprint 0 — Fundación técnica              COMPLETADO
Sprint 1 — Autenticación                  COMPLETADO
Sprint 2 — Catálogo y zonas                COMPLETADO
Sprint 3 — Pedidos y entregas              COMPLETADO
Sprint 4 — Panel Blazor y Despacho         COMPLETADO
Sprint 5 — Flutter: base y catálogo       SIGUIENTE
Sprint 6 — Flutter: direcciones/checkout  PENDIENTE
Sprint 7 — Flutter: seguimiento/historial PENDIENTE
Sprint 8 — Integración y hardening        PENDIENTE
```

El agente debe trabajar en el sprint actual antes de avanzar al siguiente, salvo que exista una dependencia técnica claramente justificada.

---

# 32. Regla especial para el Sprint 4

El Sprint 4 debe comenzar verificando que:

```text
GroceryApp.Panel
```

esté correctamente integrado en:

```text
GroceryApp.sln
```

Después:

1. configurar Blazor Server;
2. configurar DI;
3. configurar autenticación;
4. configurar autorización;
5. implementar Login;
6. implementar redirección por rol;
7. implementar MainLayout/NavMenu;
8. implementar catálogo;
9. implementar cola de pedidos;
10. implementar vista de repartidor;
11. probar flujos completos.

No saltar directamente a construir múltiples pantallas sin establecer primero autenticación y autorización.

---

# 33. Manejo de incertidumbre

Si el agente encuentra una situación en la que existen varias interpretaciones razonables:

- no inventar requisitos;
- revisar primero la hoja de ruta;
- revisar implementaciones existentes;
- elegir la opción de menor impacto;
- explicar la incertidumbre.

Si una decisión puede afectar arquitectura, seguridad, datos o reglas de negocio, debe señalarse antes de realizar un cambio destructivo.

---

# 34. Principio general

La prioridad del proyecto es:

```text
Correctitud
    >
Cumplimiento de reglas de negocio
    >
Seguridad
    >
Mantenibilidad
    >
Simplicidad
    >
Velocidad de implementación
```

La IA debe optimizar por **progreso seguro y verificable**, no por cantidad de código producido.

Un cambio pequeño y correcto es preferible a una reestructuración grande e innecesaria.

---

# 35. Regla final

Antes de considerar una tarea terminada, el agente debe poder responder afirmativamente:

```text
¿Leí la hoja de ruta?
¿Respeté AGENTS.md?
¿Inspeccioné el código existente?
¿Reutilicé lo que ya existía?
¿Evité duplicar lógica?
¿Respeté la arquitectura?
¿Respeté los roles?
¿Respeté las reglas de negocio?
¿Probé los cambios?
¿Verifiqué que no rompí lo anterior?
¿La funcionalidad cumple realmente su criterio de aceptación?
```

Si alguna respuesta es "no", la tarea no debe considerarse completamente terminada.