using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GroceryApp.Infrastructure.Seed;

/// <summary>
/// Seed mínimo de Sprint 0/1/2. Llamar desde Program.cs después de aplicar migraciones:
///   await DbSeeder.SeedAsync(dbContext, passwordHasher, environment);
/// En Development también siembra un catálogo de productos de prueba (idempotente).
/// </summary>
public static class DbSeeder
{
    // Polígono real del casco urbano, dibujado en geojson.io (Sprint 2).
    // Cerrado explícitamente con el primer punto repetido al final.
    private const string PoligonoCascoUrbano =
        "POLYGON((-86.2061965 11.8530883, -86.197957 11.8276686, -86.1771791 11.830123, " +
        "-86.1742236 11.8370478, -86.1852395 11.8547536, -86.1900031 11.8607548, " +
        "-86.2019052 11.8597941, -86.2061965 11.8530883))";

    public static async Task SeedAsync(
        GroceryAppDbContext db,
        PasswordHasher<Empleado> empleadoHasher,
        IHostEnvironment environment)
    {
        // --- Zonas: upsert por nombre, así el polígono se actualiza aunque la zona ya exista ---
        var cascoUrbano = await db.Zonas.FirstOrDefaultAsync(z => z.Nombre == "Casco urbano");
        if (cascoUrbano is null)
        {
            db.Zonas.Add(new Zona
            {
                Nombre = "Casco urbano",
                Tipo = TipoZona.CascoUrbano,
                TarifaEnvio = 30m,
                Activa = true,
                PoligonoWkt = PoligonoCascoUrbano
            });
        }
        else if (cascoUrbano.PoligonoWkt != PoligonoCascoUrbano)
        {
            cascoUrbano.PoligonoWkt = PoligonoCascoUrbano;
        }

        var municipiosAledanos = await db.Zonas.FirstOrDefaultAsync(z => z.Nombre == "Municipios aledaños");
        if (municipiosAledanos is null)
        {
            // TarifaEnvio en 0 y Activa=false hasta definir el monto (pendiente de Fase 1).
            // Sin polígono propio: por ahora cualquier pin fuera del casco urbano queda "fuera de cobertura".
            db.Zonas.Add(new Zona
            {
                Nombre = "Municipios aledaños",
                Tipo = TipoZona.MunicipioAledano,
                TarifaEnvio = 0m,
                Activa = false
            });
        }

        if (!db.Categorias.Any())
        {
            db.Categorias.AddRange(
                new Categoria { Nombre = "Granos básicos" },
                new Categoria { Nombre = "Lácteos y huevos" },
                new Categoria { Nombre = "Frutas y verduras" },
                new Categoria { Nombre = "Carnes y embutidos" },
                new Categoria { Nombre = "Panadería" },
                new Categoria { Nombre = "Bebidas" },
                new Categoria { Nombre = "Limpieza del hogar" },
                new Categoria { Nombre = "Higiene personal" },
                new Categoria { Nombre = "Abarrotes generales" }
            );
        }

        if (!db.Sucursales.Any())
        {
            db.Sucursales.Add(new Sucursal
            {
                Nombre = "Sucursal Central - Jinotepe",
                Direccion = "Del Parque Central 2c al Este, Jinotepe, Carazo",
                HorarioApertura = new TimeOnly(7, 0),
                HorarioCierre = new TimeOnly(20, 0)
            });
            await db.SaveChangesAsync();
        }

        var sucursalPrincipal = await db.Sucursales.FirstOrDefaultAsync();

        if (!db.Empleados.Any(e => e.Usuario == "admin"))
        {
            var admin = new Empleado
            {
                Nombre = "Administrador",
                Usuario = "admin",
                Rol = RolEmpleado.Admin,
                SucursalId = null
            };
            admin.PasswordHash = empleadoHasher.HashPassword(admin, "CambiarEstaClave123!");
            db.Empleados.Add(admin);
        }

        if (sucursalPrincipal != null)
        {
            if (!db.Empleados.Any(e => e.Usuario == "operador"))
            {
                var operador = new Empleado
                {
                    Nombre = "Operador Sucursal",
                    Usuario = "operador",
                    Rol = RolEmpleado.EmpleadoSucursal,
                    SucursalId = sucursalPrincipal.Id
                };
                operador.PasswordHash = empleadoHasher.HashPassword(operador, "Operador123!");
                db.Empleados.Add(operador);
            }

            if (!db.Empleados.Any(e => e.Usuario == "repartidor1"))
            {
                var repartidor = new Empleado
                {
                    Nombre = "Repartidor 1",
                    Usuario = "repartidor1",
                    Rol = RolEmpleado.Repartidor,
                    SucursalId = sucursalPrincipal.Id
                };
                repartidor.PasswordHash = empleadoHasher.HashPassword(repartidor, "Repartidor123!");
                db.Empleados.Add(repartidor);
            }
        }

        await db.SaveChangesAsync();

        // --- Catálogo de productos de prueba: SOLO en Development ---
        if (environment.IsDevelopment() && sucursalPrincipal != null)
        {
            await SeedProductosDesarrolloAsync(db, sucursalPrincipal.Id);
        }
    }

    /// <summary>
    /// Siembra un catálogo pequeño y realista de productos de prueba para desarrollo.
    /// Idempotente: verifica por nombre de producto y por (ProductoId, SucursalId).
    /// No se ejecuta en Production.
    /// </summary>
    private static async Task SeedProductosDesarrolloAsync(GroceryAppDbContext db, int sucursalId)
    {
        // (Nombre, Descripción, Categoría, Precio en C$)
        var catalogo = new (string Nombre, string Descripcion, string Categoria, decimal Precio)[]
        {
            // Granos básicos
            ("Arroz 1 lb", "Arroz blanco de primera, bolsa de 1 libra", "Granos básicos", 22.00m),
            ("Arroz 5 lb", "Arroz blanco de primera, bolsa de 5 libras", "Granos básicos", 105.00m),
            ("Frijoles rojos 1 lb", "Frijoles rojos seleccionados, bolsa de 1 libra", "Granos básicos", 35.00m),
            ("Frijoles rojos 5 lb", "Frijoles rojos seleccionados, bolsa de 5 libras", "Granos básicos", 165.00m),
            ("Azúcar 2 lb", "Azúcar blanca refinada, bolsa de 2 libras", "Granos básicos", 45.00m),
            ("Harina de maíz 2 lb", "Harina de maíz precocida, bolsa de 2 libras", "Granos básicos", 38.00m),

            // Lácteos y huevos
            ("Leche entera 1 L", "Leche entera pasteurizada, caja de 1 litro", "Lácteos y huevos", 42.00m),
            ("Leche en polvo 400 g", "Leche entera en polvo, bolsa de 400 gramos", "Lácteos y huevos", 95.00m),
            ("Huevos docena", "Docena de huevos frescos de gallina", "Lácteos y huevos", 65.00m),
            ("Queso fresco 1 lb", "Queso fresco artesanal, libra", "Lácteos y huevos", 85.00m),
            ("Crema 250 ml", "Crema ácida pasteurizada, envase de 250 ml", "Lácteos y huevos", 55.00m),

            // Frutas y verduras
            ("Tomate 1 lb", "Tomate fresco de primera, libra", "Frutas y verduras", 18.00m),
            ("Cebolla 1 lb", "Cebolla blanca fresca, libra", "Frutas y verduras", 20.00m),
            ("Chiltoma 1 lb", "Chiltoma verde fresca, libra", "Frutas y verduras", 25.00m),
            ("Plátano verde unidad", "Plátano verde para freír, unidad", "Frutas y verduras", 8.00m),

            // Carnes y embutidos
            ("Pollo entero 1 kg", "Pollo entero fresco, kilogramo", "Carnes y embutidos", 145.00m),
            ("Carne molida de res 1 lb", "Carne molida de res fresca, libra", "Carnes y embutidos", 130.00m),
            ("Chorizo criollo 1 lb", "Chorizo criollo nicaragüense, libra", "Carnes y embutidos", 110.00m),

            // Panadería
            ("Pan francés unidad", "Pan francés recién horneado, unidad", "Panadería", 5.00m),
            ("Semitas 6 unidades", "Semitas tradicionales, paquete de 6", "Panadería", 30.00m),

            // Bebidas
            ("Café molido 1 lb", "Café molido de altura, libra", "Bebidas", 120.00m),
            ("Refresco 2 L", "Refresco de cola, botella de 2 litros", "Bebidas", 55.00m),
            ("Agua purificada 5 gal", "Agua purificada, garrafón de 5 galones", "Bebidas", 60.00m),
            ("Jugo de naranja 1 L", "Jugo de naranja natural, botella de 1 litro", "Bebidas", 70.00m),

            // Limpieza del hogar
            ("Jabón de baño unidad", "Jabón de baño de tocador, unidad", "Limpieza del hogar", 25.00m),
            ("Detergente 1 kg", "Detergente en polvo para ropa, bolsa de 1 kg", "Limpieza del hogar", 85.00m),
            ("Cloro 1 L", "Cloro desinfectante, botella de 1 litro", "Limpieza del hogar", 35.00m),

            // Higiene personal
            ("Papel higiénico 4 rollos", "Papel higiénico doble hoja, paquete de 4 rollos", "Higiene personal", 55.00m),
            ("Pasta dental 100 ml", "Pasta dental con flúor, tubo de 100 ml", "Higiene personal", 65.00m),

            // Abarrotes generales
            ("Aceite vegetal 1 L", "Aceite vegetal comestible, botella de 1 litro", "Abarrotes generales", 95.00m),
            ("Atún en agua 140 g", "Atún en agua, lata de 140 gramos", "Abarrotes generales", 45.00m),
            ("Pasta espagueti 500 g", "Pasta espagueti, bolsa de 500 gramos", "Abarrotes generales", 35.00m),
            ("Galletas de soda", "Galletas de soda, paquete", "Abarrotes generales", 20.00m),
            ("Sal 1 lb", "Sal yodada, bolsa de 1 libra", "Abarrotes generales", 12.00m),
        };

        // Cargar categorías existentes en un diccionario por nombre
        var categorias = await db.Categorias.ToDictionaryAsync(c => c.Nombre, c => c.Id);

        foreach (var item in catalogo)
        {
            if (!categorias.TryGetValue(item.Categoria, out var categoriaId))
            {
                // Si la categoría no existe (no debería pasar porque el seed base las crea),
                // se omite el producto para no violar la FK.
                continue;
            }

            var producto = await db.Productos.FirstOrDefaultAsync(p => p.Nombre == item.Nombre);
            if (producto is null)
            {
                producto = new Producto
                {
                    Nombre = item.Nombre,
                    Descripcion = item.Descripcion,
                    CategoriaId = categoriaId,
                    FotoUrl = null
                };
                db.Productos.Add(producto);
                await db.SaveChangesAsync();
            }

            var productoSucursal = await db.ProductosSucursal
                .FirstOrDefaultAsync(ps => ps.ProductoId == producto.Id && ps.SucursalId == sucursalId);

            if (productoSucursal is null)
            {
                db.ProductosSucursal.Add(new ProductoSucursal
                {
                    ProductoId = producto.Id,
                    SucursalId = sucursalId,
                    Precio = item.Precio,
                    StockDisponible = true
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
