using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DACS_PetShop.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Product> Products { get; set; }
    public DbSet<Size> Sizes { get; set; }  // Thêm DbSet cho Size
    public DbSet<ProductSize> ProductSizes { get; set; }  // Thêm DbSet cho ProductSize
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ProductImage constraint: Maximum 3 images per product
        modelBuilder.Entity<ProductImage>()
            .HasIndex(pi => new { pi.ProductId, pi.DisplayOrder })
            .IsUnique();
        // Mối quan hệ giữa Product và Size thông qua ProductSize
        modelBuilder.Entity<ProductSize>()
            .HasOne(ps => ps.Product)
            .WithMany(p => p.ProductSizes)
            .HasForeignKey(ps => ps.ProductId);

        modelBuilder.Entity<ProductSize>()
            .HasOne(ps => ps.Size)
            .WithMany(s => s.ProductSizes)
            .HasForeignKey(ps => ps.SizeId);
        // CartItem constraint: Quantity must be positive
        modelBuilder.Entity<CartItem>()
            .Property(ci => ci.Quantity)
            .HasDefaultValue(1);

        // OrderDetail constraint: Quantity and UnitPrice must be positive
        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.Quantity)
            .HasDefaultValue(1);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.UnitPrice)
            .HasColumnType("decimal(18,2)");

        // Payment constraint: Amount must be positive
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        // Review constraint: Rating between 1 and 5
        modelBuilder.Entity<Review>()
            .Property(r => r.Rating)
            .HasDefaultValue(1);
        // Mối quan hệ CartItem - Size
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Size)
            .WithMany()
            .HasForeignKey(ci => ci.SizeId);

        // Mối quan hệ OrderDetail - Size
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Size)
            .WithMany()
            .HasForeignKey(od => od.SizeId);
        // Mối quan hệ giữa Product và Review
        modelBuilder.Entity<Review>()
           .HasOne(r => r.Product)
           .WithMany(p => p.Reviews)
           .HasForeignKey(r => r.ProductId);
    }
}