using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;

namespace GroceryApp.Infrastructure.Seed;

/// <summary>
/// Seed mínimo de Sprint 0. Ajustá los nombres reales de barrios/municipios
/// de Carazo antes de correrlo contra producción.
/// Llamar desde Program.cs después de aplicar migraciones:
///   await DbSeeder.SeedAsync(dbContext);
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(GroceryAppDbContext db)
    {
        if (!db.Zonas.Any())
        {
            db.Zonas.AddRange(
                new Zona { Nombre = "Casco urbano", Tipo = TipoZona.CascoUrbano, TarifaEnvio = 30m, Activa = true },
                new Zona { Nombre = "Municipios aledaños", Tipo = TipoZona.MunicipioAledano, TarifaEnvio = 0m, Activa = false }
                // TarifaEnvio de municipios aledaños en 0 y Activa=false hasta definir el monto (pendiente de Fase 1).
            );
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

        await db.SaveChangesAsync();
    }
}
