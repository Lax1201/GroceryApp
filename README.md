# GroceryApp

## Sprint 2 — Catálogo y Zonas (agregado en esta rama)

### Endpoints nuevos
- `GET /api/v1/catalogo/categorias` — público, sin auth.
- `POST /api/v1/direcciones` · `GET /api/v1/direcciones` · `PUT /api/v1/direcciones/{id}` · `DELETE /api/v1/direcciones/{id}` — requieren rol `Cliente` (token de Sprint 1). Al crear/editar, el backend calcula la zona automáticamente a partir del pin (lat/long); si el punto no cae en ninguna zona activa, devuelve `422` con "fuera de cobertura por ahora".
- `GET /api/v1/panel/sucursales` · `POST /api/v1/panel/sucursales` — requieren rol `Admin`.
- `GET /api/v1/panel/productos/sucursal/{sucursalId}` · `POST /api/v1/panel/productos` · `PUT /api/v1/panel/productos/{id}` · `PUT /api/v1/panel/productos/{id}/sucursal/{sucursalId}` (precio/stock) · `POST /api/v1/panel/productos/{id}/foto` (sube archivo real) — requieren rol `Admin`.

### ⚠️ Paso manual obligatorio antes de poder crear direcciones: el polígono de la zona

Elegimos que la zona de entrega se calcule con un **polígono real** (no un radio, no un servicio pago). Eso significa que **el sistema no puede adivinar los límites del casco urbano de Carazo** — hay que dibujarlos una vez, a mano:

1. Entrá a **https://geojson.io** (gratis, sin login).
2. Con la herramienta de polígono, dibujá el borde real del casco urbano sobre el mapa (Jinotepe o el municipio que corresponda).
3. A la derecha te va a mostrar el GeoJSON del polígono que dibujaste. Copiá los pares de coordenadas `[longitud, latitud]`.
4. Convertilos a formato WKT (el orden es igual: longitud primero, latitud después):
   ```
   POLYGON((lon1 lat1, lon2 lat2, lon3 lat3, ..., lon1 lat1))
   ```
   (el primer y último punto deben ser iguales para cerrar el polígono).
5. Actualizá el seed en `GroceryApp.Infrastructure/Seed/DbSeeder.cs`, en la zona "Casco urbano", agregando `PoligonoWkt = "POLYGON((...))"`.
6. Si ya corriste el seed antes (la zona ya existe en tu base), no se va a volver a insertar — actualizá el registro directo en la tabla `Zonas` con ese WKT, o borrá la tabla y dejá que el seed la vuelva a crear.

**Mientras no hagas esto, todo intento de crear una dirección va a fallar con "fuera de cobertura"** — es el comportamiento esperado, no un bug.

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


