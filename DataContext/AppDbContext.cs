using IT_ASSET.Models;
using IT_ASSET.Models.Logs;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Asset_logs> asset_Logs { get; set; }
    public DbSet<User_logs> user_logs { get; set; }
    public DbSet<UserAccountabilityList> user_accountability_lists { get; set; }
    public DbSet<ComputerComponents> computer_components { get; set; }
    public DbSet<Computer> computers { get; set; } // Add this line to the context


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure one-to-many relationship (User -> Asset)
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.owner)
            .WithMany(u => u.assets)
            .HasForeignKey(a => a.owner_id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Computer>()
            .HasOne(a => a.owner)
            .WithMany(u => u.computer)
            .HasForeignKey(a =>a.owner_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure one-to-many relationship (asset -> asset_logs)
        modelBuilder.Entity<Asset_logs>()
            .HasOne(al => al.assets)
            .WithMany()
            .HasForeignKey(al => al.asset_id);

        //configure one-to-many relationship (user -> user_logs)
        modelBuilder.Entity<User_logs>()
            .HasOne(a => a.user)
            .WithMany()
            .HasForeignKey(a => a.user_id);

        // Configure UserAccountabilityList without cascading deletes
        modelBuilder.Entity<UserAccountabilityList>()
            .HasKey(ual => ual.id);

        modelBuilder.Entity<UserAccountabilityList>()
            .HasOne(ual => ual.owner)
            .WithMany()
            .HasForeignKey(ual => ual.owner_id)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ComputerComponents>()
            .HasOne(cc => cc.owner)  // Assuming `owner` is a navigation property in `ComputerComponents`
            .WithMany(u => u.computer_components)  // Assuming `computer_components` is a navigation property in `User`
            .HasForeignKey(cc => cc.owner_id)
            .OnDelete(DeleteBehavior.Cascade);


        //modelBuilder.Entity<UserAccountabilityList>()
        //    .HasOne(ual => ual.asset)
        //    .WithMany()
        //    .HasForeignKey(ual => ual.asset_id)
        //    .OnDelete(DeleteBehavior.NoAction);

    }
}
