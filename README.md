# GroceryApp

## Sprint 1 — Autenticación (agregado en esta rama)
- **Endpoints nuevos** (`/api/v1/auth/...`):
  - `POST /api/v1/auth/cliente/registro` — nombre, teléfono (formato NI), password (mín. 6), email opcional.
  - `POST /api/v1/auth/cliente/login` — teléfono + password.
  - `POST /api/v1/auth/empleado/login` — usuario + password (rol viene en el JWT: EmpleadoSucursal, Repartidor o Admin).
  - **No hay registro de empleado**: esas cuentas las crea un Admin desde el panel (Sprint 4). Por ahora hay un Admin de prueba sembrado automáticamente: usuario `admin`, password `CambiarEstaClave123!` — **cambiala en cuanto exista una pantalla real de gestión de empleados**.
- **Recuperación de contraseña**: pospuesta a un sprint futuro (decisión tomada explícitamente, no es un olvido).
- **Nuevo proyecto/pieza**: `GroceryApp.Application/Common/IAppDbContext.cs` — abstracción mínima para que `Application` no dependa de `Infrastructure` (necesario para que `ClienteAuthService`/`EmpleadoAuthService` puedan consultar la base de datos sin romper la dirección de dependencias).
- **JWT real**: `GroceryApp.Infrastructure/Security/JwtTokenGenerator.cs` genera el token con claims de rol y, para empleados, `sucursalId` si aplica. Configuración de expiración en `appsettings.json` → `Jwt:ExpiraHoras` (168 = 7 días por defecto).
- **Swagger** ahora tiene los 3 endpoints visibles con el botón "Authorize" para probar rutas protegidas más adelante.

No hubo cambios de esquema de base de datos en este sprint (Cliente/Empleado ya tenían `PasswordHash`), así que **no hace falta una migración nueva**.

## Cómo probar
1. `dotnet run --project GroceryApp.Api`
2. Swagger → `POST /api/v1/auth/cliente/registro` con un teléfono de 8 dígitos y password de 6+ caracteres → devuelve un JWT.
3. `POST /api/v1/auth/empleado/login` con `admin` / `CambiarEstaClave123!` → devuelve un JWT con rol Admin.

---

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


