using Inventario.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Data;

public class InventarioDbContext(DbContextOptions<InventarioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetLog> AssetLogs => Set<AssetLog>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<SupplyMovement> SupplyMovements => Set<SupplyMovement>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanItem> LoanItems => Set<LoanItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
#pragma warning disable CS0618 // Npgsql: UseXminAsConcurrencyToken sigue siendo el mecanismo que excluye la columna de sistema xmin de las migraciones.
        // Esquema propio dentro de la base de datos de operaciones.
        b.HasDefaultSchema("inventory");

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(60);
            e.Property(x => x.Role).HasMaxLength(30);
        });

        b.Entity<Room>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120);
        });

        b.Entity<Asset>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Verification);
            e.HasIndex(x => new { x.RoomId, x.Verification });
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Condition).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Verification).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Room).WithMany(r => r.Assets).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Logs).WithOne().HasForeignKey(l => l.AssetId).OnDelete(DeleteBehavior.Cascade);
            // xmin es una columna de sistema de PostgreSQL: detecta ediciones concurrentes sin agregar campos.
            e.UseXminAsConcurrencyToken();
        });

        b.Entity<AssetLog>(e => e.HasIndex(x => x.AssetId));

        b.Entity<Supply>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20);
            e.HasMany(x => x.Movements).WithOne().HasForeignKey(m => m.SupplyId).OnDelete(DeleteBehavior.Cascade);
            e.UseXminAsConcurrencyToken();
        });

        b.Entity<SupplyMovement>(e =>
        {
            e.HasIndex(x => x.SupplyId);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<Loan>(e =>
        {
            e.HasIndex(x => new { x.ReturnedAt, x.DueDate });
            e.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.LoanId).OnDelete(DeleteBehavior.Cascade);
            e.UseXminAsConcurrencyToken();
        });

        b.Entity<LoanItem>(e =>
        {
            e.HasIndex(x => x.AssetId);
            e.Property(x => x.ReturnCondition).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.UseXminAsConcurrencyToken();
        });
#pragma warning restore CS0618
    }
}
