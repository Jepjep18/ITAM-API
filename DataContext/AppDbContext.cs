using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Asset> Assets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure one-to-many relationship (User -> Asset)
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.owner)
            .WithMany(u => u.assets)
            .HasForeignKey(a => a.owner_id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
