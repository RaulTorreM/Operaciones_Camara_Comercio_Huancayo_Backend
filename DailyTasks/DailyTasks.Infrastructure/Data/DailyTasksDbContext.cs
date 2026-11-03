using DailyTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Infrastructure.Data;

public class DailyTasksDbContext(DbContextOptions<DailyTasksDbContext> options) : DbContext(options)
{
    public DbSet<DailyTaskUser> Users => Set<DailyTaskUser>();
    public DbSet<DailyTask> Tasks => Set<DailyTask>();
    public DbSet<DailyTaskHistory> TaskHistory => Set<DailyTaskHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
#pragma warning disable CS0618 // Npgsql: UseXminAsConcurrencyToken sigue siendo el mecanismo para excluir xmin de las migraciones.
        // Esquema propio dentro de la base de datos de operaciones.
        b.HasDefaultSchema("dailytasks");

        b.Entity<DailyTaskUser>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Area);
            e.Property(x => x.Username).HasMaxLength(60);
            e.Property(x => x.FullName).HasMaxLength(120);
            e.Property(x => x.Area).HasMaxLength(60);
            e.Property(x => x.Role).HasMaxLength(20);
        });

        b.Entity<DailyTask>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Area).HasMaxLength(60);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.CreatedBy)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(x => x.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.DeletedBy)
                .WithMany()
                .HasForeignKey(x => x.DeletedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.History)
                .WithOne(h => h.Task)
                .HasForeignKey(h => h.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.DeletedAt, x.Status, x.DueDate });
            e.HasIndex(x => new { x.AssignedToId, x.Status });
            e.HasIndex(x => new { x.Area, x.Status });
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.CreatedOn);
            e.HasIndex(x => x.DueDate);
            e.HasIndex(x => x.CompletedOn);

            // Detecta ediciones concurrentes (por ejemplo, dos personas moviendo la misma tarjeta).
            e.UseXminAsConcurrencyToken();
        });

        b.Entity<DailyTaskHistory>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(30);
            e.Property(x => x.Field).HasMaxLength(40);

            e.HasOne(x => x.ChangedBy)
                .WithMany(u => u.Changes)
                .HasForeignKey(x => x.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.TaskId, x.ChangedAt });
        });
#pragma warning restore CS0618
    }
}
