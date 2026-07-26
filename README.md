# GroceryApp

## Sprint 3 — Pedidos y Entregas

No hay cambios de esquema de base de datos — **no hace falta migración nueva**.

### Cambio de diseño: transiciones de estado encapsuladas
`Pedido` y `Entrega` ya no exponen `Estado` con setter público — ahora tienen métodos (`Confirmar()`, `MarcarListo()`, `MarcarEntregado()`, etc.) que validan el estado de origen y lanzan `DomainException` si la transición no es válida (ej. no se puede marcar "listo" un pedido que sigue "pendiente"). Esto cumple la mejora que habíamos dejado pendiente en la consolidación de Sprint 0. Cada servicio de `Application` atrapa esa excepción y la convierte en un `Result.Fallido(...)` — el dominio no conoce el patrón `Result`, mantiene cero dependencias externas.

### Modelo de asignación de repartidores: pool (autoservicio)
Decisión tomada pensando en escalar de 1 a varios repartidores por ciudad: el flujo normal es que **el repartidor "toma" un pedido disponible él mismo** (como Uber Eats/Rappi), no que un empleado se lo asigne. También dejamos una asignación manual de respaldo por si hace falta. La concurrencia real (dos repartidores tocando "tomar" al mismo tiempo) la resuelve el índice único de `Entregas.PedidoId` que ya existía desde Fase 5 — el chequeo previo en código es solo para dar un mensaje amigable, no la protección real.

### Endpoints nuevos

**Cliente** (`/api/v1/pedidos`):
- `POST /api/v1/pedidos` — checkout. Valida catálogo, stock, que todos los productos sean de la misma sucursal, y horario de atención (bloqueo fuera de horario, Fase 1).
- `GET /api/v1/pedidos/{id}` · `GET /api/v1/pedidos/historial` · `GET /api/v1/pedidos/{id}/seguimiento`
- `PUT /api/v1/pedidos/{id}/cancelar` — solo si el pedido sigue en estado Pendiente (regla de Fase 1).

**Empleado de sucursal / Admin** (`/api/v1/panel/pedidos`):
- `GET /api/v1/panel/pedidos?estado=` — Admin ve todas las sucursales; EmpleadoSucursal solo la suya (vía claim `sucursalId` del JWT).
- `PUT /{id}/confirmar` · `/rechazar` · `/iniciar-preparacion` · `/marcar-listo`
- `DELETE /{id}/items/{itemId}` — producto faltante al preparar: se quita y se recalcula el total.
- `PUT /{id}/asignar-repartidor` — respaldo manual (no es el flujo normal).

**Repartidor** (`/api/v1/panel/entregas`):
- `GET /disponibles` — pedidos "Listo" de su sucursal, sin tomar todavía (el pool).
- `POST /{pedidoId}/tomar` — se autoasigna el pedido.
- `GET /mias` — sus entregas activas.
- `PUT /{id}/en-camino` · `/entregado` · `/no-entregado` — este último incrementa `NoShowCount` del cliente (regla de Fase 1).

### Cómo probar de punta a punta
1. Cliente: crear pedido con productos de Sprint 2 y una dirección con cobertura.
2. Admin o EmpleadoSucursal (necesitás crear un empleado con rol `EmpleadoSucursal` directo en la base por ahora — no hay pantalla todavía): confirmar → iniciar preparación → marcar listo.
3. Repartidor (mismo caso: crealo directo en la base con rol `Repartidor` y el `SucursalId` de tu sucursal): ver disponibles → tomar → en camino → entregado.
4. Confirmá en la tabla `Pedidos` que el `Estado` quedó en `Entregado` y coincide con el de `Entregas`.

---

## Sprint 2 — Catálogo y Zonas

### Endpoints nuevos
- `GET /api/v1/catalogo/categorias` — público, sin auth.
- `POST /api/v1/direcciones` · `GET /api/v1/direcciones` · `PUT /api/v1/direcciones/{id}` · `DELETE /api/v1/direcciones/{id}` — requieren rol `Cliente` (token de Sprint 1). Al crear/editar, el backend calcula la zona automáticamente a partir del pin (lat/long); si el punto no cae en ninguna zona activa, devuelve `422` con "fuera de cobertura por ahora".
- `GET /api/v1/panel/sucursales` · `POST /api/v1/panel/sucursales` — requieren rol `Admin`.
- `GET /api/v1/panel/productos/sucursal/{sucursalId}` · `POST /api/v1/panel/productos` · `PUT /api/v1/panel/productos/{id}` · `PUT /api/v1/panel/productos/{id}/sucursal/{sucursalId}` (precio/stock) · `POST /api/v1/panel/productos/{id}/foto` (sube archivo real) — requieren rol `Admin`.

### El polígono del casco urbano

Ya está cargado en `DbSeeder.cs` (dibujado en geojson.io). El seed hace *upsert*: si la zona "Casco urbano" ya existía en tu base sin polígono, se actualiza sola la próxima vez que corras la API — no hace falta borrar nada a mano.

Si en el futuro necesitás redibujarlo (el área creció, cambiaron los límites, etc.), repetí el proceso en geojson.io y actualizá la constante `PoligonoCascoUrbano` en `DbSeeder.cs` — el upsert se encarga del resto.

### Migración nueva requerida
Este sprint agrega la columna `Zona.PoligonoWkt`:
```bash
cd GroceryApp.Api
dotnet ef migrations add AgregarPoligonoAZona --project ../GroceryApp.Infrastructure --startup-project .
```

### Fotos de productos
Se guardan como archivos reales en `GroceryApp.Api/wwwroot/uploads/productos/`, servidos como estáticos (`app.UseStaticFiles()`). La carpeta está versionada vacía (`.gitkeep`); las imágenes subidas quedan ignoradas por git — en el VPS vas a necesitar backupear esa carpeta aparte si querés conservar las fotos (no viven en la base de datos, solo la URL).

### Nota de diseño: Admin de prueba y Sucursal
Para probar `POST /api/v1/panel/productos` necesitás primero crear una Sucursal real con `POST /api/v1/panel/sucursales` (usando el token del Admin de prueba de Sprint 1) y anotar el `id` que te devuelve — lo vas a necesitar en el `sucursalId` de cada producto.

---

## Sprint 1 — Autenticación
- **Endpoints** (`/api/v1/auth/...`): `cliente/registro`, `cliente/login`, `empleado/login`.
- **No hay registro de empleado**: esas cuentas las crea un Admin desde el panel (Sprint 4). Admin de prueba sembrado: usuario `admin`, password `CambiarEstaClave123!` — **cambiala en cuanto exista una pantalla real de gestión de empleados**.
- **Recuperación de contraseña**: pospuesta a un sprint futuro (decisión tomada explícitamente).
- `GroceryApp.Application/Common/IAppDbContext.cs` — abstracción mínima para que `Application` no dependa de `Infrastructure`.
- `GroceryApp.Infrastructure/Security/JwtTokenGenerator.cs` — genera el JWT con claims de rol y `sucursalId` si aplica.



## Sprint 0 — Fundación técnica y consolidación

Estructura base según Fase 4/5 de la hoja de ruta, **más la ronda de consolidación técnica** (Application layer, enums legibles, manejo de errores, versionado de API).

### Qué se consolidó
- **`GroceryApp.Application`**: patrón `Result`/`Result<T>` + `IAppDbContext`.
- **Enums guardados como texto** en SQL Server (`Rol`, `Estado` de Pedido, `Estado` de Entrega, `Tipo` de Zona).
- **Autenticación**: *Identity liviano* — `PasswordHasher<Cliente>` y `PasswordHasher<Empleado>`, sin el framework completo de ASP.NET Core Identity.
- **Manejo global de errores**: `AddProblemDetails()` + `UseExceptionHandler()`.
- **Versionado de API**: `/api/v1/...` vía `Asp.Versioning.Mvc`.
- **Cascadas de borrado corregidas**: `Pedido→Cliente`, `Pedido→Direccion`, `PedidoItem→Producto` y `Direccion→Zona` son `Restrict` (no cascada) — evita el error de SQL Server por múltiples caminos de cascada hacia `Pedidos`, y protege el historial de pedidos de borrados accidentales en cascada.

## 1. Requisitos
- .NET 8 SDK instalado.
- SQL Server (local, Docker, o el que uses en desarrollo).
- Herramienta EF Core: `dotnet tool install --global dotnet-ef` (si no la tenés).

## 2. Restaurar y compilar
```bash
dotnet restore
dotnet build
```

## 3. Configurar la cadena de conexión y el JWT
```bash
cd GroceryApp.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=GroceryAppDb;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-secreta-de-al-menos-32-caracteres"
```

## 4. Migraciones
Ya existe `InitialCreate` en `GroceryApp.Infrastructure/Migrations`. Se aplica sola al arrancar la API (`Program.cs` corre `db.Database.MigrateAsync()`).

## 5. Correr la API
```bash
dotnet run --project GroceryApp.Api
```

## 6. Pendiente para cerrar Sprint 0 (fuera del alcance de este código)
- **VPS con HTTPS**: Nginx como reverse proxy hacia Kestrel + Let's Encrypt. Si es Windows: IIS + ASP.NET Core Hosting Bundle + win-acme.
- Definir el monto real de `TarifaEnvio` para "Municipios aledaños" antes de activarla.
- Crear el primer registro real en `Sucursales` con el horario real.

## Recomendaciones de la consolidación que quedaron pendientes (no bloquean Sprint 1/2)
- CHECK constraint para `Empleado.SucursalId` NULL solo si `Rol=Admin`.
- Mover la configuración Fluent API del `DbContext` a clases `IEntityTypeConfiguration<T>` por entidad.
- Health checks (`/health`).
- Encapsular las transiciones de estado de `Pedido` en métodos en vez de setter directo (se evalúa en Sprint 3).

## Qué NO está todavía (a propósito)
Catálogo/zonas (Sprint 2), pedidos/entregas (Sprint 3), panel Blazor (Sprint 4) y la app Flutter (Sprints 5-7) van en las siguientes entregas, uno a la vez.

## Estructura
```
GroceryApp.sln
GroceryApp.Domain/           → entidades y enums, sin dependencias externas
GroceryApp.Application/      → Result pattern, IAppDbContext, IJwtTokenGenerator, ClienteAuthService, EmpleadoAuthService
GroceryApp.Infrastructure/   → DbContext, configuración EF Core, seed, JwtTokenGenerator
GroceryApp.Api/              → Program.cs, controllers, contratos, JWT, versionado, ProblemDetails, Swagger
```
`GroceryApp.Shared` y `GroceryApp.Panel` se agregan cuando haya lógica real que poner ahí (Sprint 4 en adelante).


