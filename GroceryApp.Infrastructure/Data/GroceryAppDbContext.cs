using GroceryApp.Application.Common;
using GroceryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryApp.Infrastructure.Data;

public class GroceryAppDbContext : DbContext, IAppDbContext
{
    public GroceryAppDbContext(DbContextOptions<GroceryAppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Direccion> Direcciones => Set<Direccion>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoSucursal> ProductosSucursal => Set<ProductoSucursal>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();
    public DbSet<Entrega> Entregas => Set<Entrega>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Cliente ---
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Telefono)
            .IsUnique();

        // --- Empleado ---
        modelBuilder.Entity<Empleado>()
            .HasIndex(e => e.Usuario)
            .IsUnique();

        modelBuilder.Entity<Empleado>()
            .Property(e => e.Rol)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Sucursal)
            .WithMany(s => s.Empleados)
            .HasForeignKey(e => e.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- ProductoSucursal: precio > 0 y único por (Producto, Sucursal) ---
        modelBuilder.Entity<ProductoSucursal>()
            .HasIndex(ps => new { ps.ProductoId, ps.SucursalId })
            .IsUnique();

        modelBuilder.Entity<ProductoSucursal>()
            .Property(ps => ps.Precio)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<ProductoSucursal>()
            .ToTable(t => t.HasCheckConstraint("CK_ProductoSucursal_Precio", "[Precio] > 0"));

        // --- Pedido ---
        modelBuilder.Entity<Pedido>()
            .HasIndex(p => new { p.SucursalId, p.Estado });

        modelBuilder.Entity<Pedido>()
            .HasIndex(p => p.ClienteId);

        modelBuilder.Entity<Pedido>()
            .Property(p => p.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Pedido>()
            .Property(p => p.TarifaEnvio).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<Pedido>()
            .Property(p => p.Subtotal).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<Pedido>()
            .Property(p => p.Total).HasColumnType("decimal(10,2)");

        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Sucursal)
            .WithMany(s => s.Pedidos)
            .HasForeignKey(p => p.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict (no cascada): sin esto, SQL Server rechaza la migración porque
        // existían dos caminos de cascada hacia Pedidos (Cliente→Pedido directo,
        // y Cliente→Direccion→Pedido). Además, nunca queremos que borrar un
        // Cliente o una Direccion borre pedidos históricos.
        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Cliente)
            .WithMany(c => c.Pedidos)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Direccion)
            .WithMany()
            .HasForeignKey(p => p.DireccionId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- PedidoItem: cantidad > 0, precio congelado ---
        modelBuilder.Entity<PedidoItem>()
            .Property(pi => pi.PrecioUnitario).HasColumnType("decimal(10,2)");

        modelBuilder.Entity<PedidoItem>()
            .HasOne(pi => pi.Producto)
            .WithMany()
            .HasForeignKey(pi => pi.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PedidoItem>()
            .ToTable(t => t.HasCheckConstraint("CK_PedidoItem_Cantidad", "[Cantidad] > 0"));

        modelBuilder.Entity<PedidoItem>()
            .Ignore(pi => pi.Subtotal); // calculado en memoria, no columna

        // --- Entrega: 1 a 1 con Pedido ---
        modelBuilder.Entity<Entrega>()
            .HasIndex(e => e.PedidoId)
            .IsUnique();

        modelBuilder.Entity<Entrega>()
            .Property(e => e.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.Pedido)
            .WithOne(p => p.Entrega)
            .HasForeignKey<Entrega>(e => e.PedidoId);

        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.Repartidor)
            .WithMany(emp => emp.EntregasAsignadas)
            .HasForeignKey(e => e.RepartidorId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Zona ---
        modelBuilder.Entity<Zona>()
            .Property(z => z.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<Zona>()
            .Property(z => z.TarifaEnvio).HasColumnType("decimal(10,2)");

        modelBuilder.Entity<Direccion>()
            .HasOne(d => d.Zona)
            .WithMany(z => z.Direcciones)
            .HasForeignKey(d => d.ZonaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
