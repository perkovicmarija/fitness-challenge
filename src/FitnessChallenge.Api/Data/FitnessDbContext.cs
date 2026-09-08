using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Data;

public class FitnessDbContext(DbContextOptions<FitnessDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(100);
            user.Property(u => u.LastName).HasMaxLength(100);
            user.Property(u => u.NormalizedName).HasMaxLength(201);
            user.HasIndex(u => u.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<Activity>(activity =>
        {
            // Three decimal places matches the assignment's own 42.195. Finer precision cannot
            // change a point total, because a thousandth of a kilometre is at most 0.1 points
            // and the flooring discards it.
            activity.Property(a => a.Distance).HasPrecision(9, 3);
            activity.Property(a => a.Duration).HasMaxLength(16);
            activity.HasIndex(a => new { a.UserId, a.OccurredAtUtc });
        });
    }
}
