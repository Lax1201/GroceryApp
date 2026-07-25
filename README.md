# GroceryApp — Sprint 0 (consolidado)

Estructura base según Fase 4/5 de la hoja de ruta, **más la ronda de consolidación técnica** (Application layer, enums legibles, manejo de errores, versionado de API). Falta correr esto en tu máquina porque acá no tengo acceso al SDK de .NET ni a NuGet.

## Qué cambió en la consolidación
- **Nuevo proyecto `GroceryApp.Application`**: solo contiene el patrón `Result`/`Result<T>` por ahora. Los primeros servicios reales (`AuthService`, etc.) se agregan en Sprint 1 — no antes, para no anticipar funcionalidad de negocio.
- **Enums guardados como texto** en SQL Server (`Rol`, `Estado` de Pedido, `Estado` de Entrega, `Tipo` de Zona) en vez de números — legibles si abrís la tabla directo en SSMS.
- **Autenticación**: se decidió *Identity liviano* — `PasswordHasher<Cliente>` y `PasswordHasher<Empleado>` registrados en `Program.cs`, sin el framework completo de ASP.NET Core Identity (sin `AspNetUsers`, sin `UserManager`). El hashing real de contraseñas se usa recién en Sprint 1.
- **Manejo global de errores**: `AddProblemDetails()` + `UseExceptionHandler()` — cualquier excepción no controlada devuelve una respuesta HTTP consistente en vez del error crudo de ASP.NET.
- **Versionado de API**: configurado con `Asp.Versioning.Mvc`, rutas con prefijo `/api/v{version}/...` listas para cuando se cree el primer controller en Sprint 1.
- **Swagger** ahora incluye el botón "Authorize" para pegar el JWT y probar endpoints protegidos sin copiar el token a mano.

## 1. Requisitos
- .NET 8 SDK instalado.
- SQL Server (local, Docker, o el que uses en desarrollo).
- Herramienta EF Core: `dotnet tool install --global dotnet-ef` (si no la tenés).

## 2. Restaurar y compilar
```bash
cd GroceryApp
dotnet restore
dotnet build
```

## 3. Configurar la cadena de conexión y el JWT
Editá `GroceryApp.Api/appsettings.json` (o mejor, usá `dotnet user-secrets` para no commitear secretos):
```bash
cd GroceryApp.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=GroceryAppDb;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-secreta-de-al-menos-32-caracteres"
```

## 4. Crear la primera migración
```bash
cd GroceryApp.Api
dotnet ef migrations add InitialCreate --project ../GroceryApp.Infrastructure --startup-project .
```
Como todavía no corriste ninguna migración antes, esta ya incluye los enums como texto — no hay que migrar datos existentes.

## 5. Correr la API
```bash
dotnet run --project GroceryApp.Api
```
Al terminar deberías tener:
- Swagger en `/swagger` con botón "Authorize" (solo en Development).
- Tabla `Zonas` con "Casco urbano" (tarifa 30 — **ajustá el monto real**) y "Municipios aledaños" (inactiva).
- 9 categorías base cargadas.
- Columnas de enum (`Rol`, `Estado`, `Tipo`) guardadas como texto legible.

## 6. Pendiente para cerrar Sprint 0 (fuera del alcance de este código)
- **VPS con HTTPS**: Nginx como reverse proxy hacia Kestrel + Let's Encrypt (`certbot --nginx`). Si es Windows: IIS + ASP.NET Core Hosting Bundle + win-acme.
- Definir el monto real de `TarifaEnvio` para "Municipios aledaños" antes de activarla.
- Crear el primer registro real en `Sucursales` con el horario real.

## Recomendaciones de la consolidación que quedaron pendientes (no bloquean Sprint 1)
- CHECK constraint para `Empleado.SucursalId` NULL solo si `Rol=Admin`.
- Mover la configuración Fluent API del `DbContext` a clases `IEntityTypeConfiguration<T>` por entidad.
- Health checks (`/health`).
- Encapsular las transiciones de estado de `Pedido` en métodos en vez de setter directo (se evalúa en Sprint 3 cuando se implemente la lógica de pedidos).

## Qué NO está en este entregable (a propósito)
Autenticación real (Sprint 1), catálogo/zonas (Sprint 2), pedidos/entregas (Sprint 3), panel Blazor (Sprint 4) y la app Flutter (Sprints 5-7) van en las siguientes entregas, uno a la vez.

## Estructura
```
GroceryApp.sln
GroceryApp.Domain/           → entidades y enums, sin dependencias externas
GroceryApp.Application/      → casos de uso (Result pattern por ahora; servicios desde Sprint 1)
GroceryApp.Infrastructure/   → DbContext, configuración EF Core, seed
GroceryApp.Api/              → Program.cs, JWT, versionado, ProblemDetails, Swagger
```
`GroceryApp.Shared` y `GroceryApp.Panel` se agregan cuando haya lógica real que poner ahí (Sprint 4 en adelante).

