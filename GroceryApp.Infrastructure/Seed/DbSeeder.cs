using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Infrastructure.Seed;

/// <summary>
/// Seed mínimo de Sprint 0/1/2. Llamar desde Program.cs después de aplicar migraciones:
///   await DbSeeder.SeedAsync(dbContext, passwordHasher);
/// </summary>
public static class DbSeeder
{
    // Polígono real del casco urbano, dibujado en geojson.io (Sprint 2).
    // Cerrado explícitamente con el primer punto repetido al final.
    private const string PoligonoCascoUrbano =
        "POLYGON((-86.2061965 11.8530883, -86.197957 11.8276686, -86.1771791 11.830123, " +
        "-86.1742236 11.8370478, -86.1852395 11.8547536, -86.1900031 11.8607548, " +
        "-86.2019052 11.8597941, -86.2061965 11.8530883))";

    public static async Task SeedAsync(GroceryAppDbContext db, PasswordHasher<Empleado> empleadoHasher)
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

        if (!db.Empleados.Any())
        {
            // Cuenta de prueba para validar el login de empleado en Sprint 1.
            // CAMBIAR el usuario/contraseña reales apenas exista una pantalla de gestión (Sprint 4).
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

        await db.SaveChangesAsync();
    }
}
