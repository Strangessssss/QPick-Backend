

using Microsoft.EntityFrameworkCore;
using Qpick.Store.Entities;

namespace Qpick;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Brand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId);

        modelBuilder.Entity<User>()
            .HasMany(c => c.CartProducts)
            .WithOne()
            .HasForeignKey(u => u.UserId);
        
        modelBuilder.Entity<User>()
            .HasMany(c => c.SavedProducts)
            .WithMany();
        
        modelBuilder.Entity<CartProduct>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId);
        
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Products)
            .WithMany();
    }
    
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CartProduct> CartProducts { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }

}