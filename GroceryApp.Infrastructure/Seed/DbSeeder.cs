using GroceryApp.Domain.Entities;
using GroceryApp.Domain.Enums;
using GroceryApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace GroceryApp.Infrastructure.Seed;

/// <summary>
/// Seed mínimo de Sprint 0/1. Ajustá los nombres reales de barrios/municipios
/// de Carazo antes de correrlo contra producción.
/// Llamar desde Program.cs después de aplicar migraciones:
///   await DbSeeder.SeedAsync(dbContext, passwordHasher);
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(GroceryAppDbContext db, PasswordHasher<Empleado> empleadoHasher)
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

