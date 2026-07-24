using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DomPizzaria.Models;

namespace DomPizzaria.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Complement> Complements => Set<Complement>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<DeliveryArea> DeliveryAreas => Set<DeliveryArea>();
    public DbSet<DeliveryPerson> DeliveryPersons => Set<DeliveryPerson>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
        });

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DeliveryFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Discount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Total).HasColumnType("decimal(10,2)");
        });

        builder.Entity<Complement>(entity =>
        {
            entity.ToTable("Complements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
        });

        builder.Entity<Coupon>(entity =>
        {
            entity.ToTable("Coupons");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(10,2)");
            entity.Property(e => e.MinOrder).HasColumnType("decimal(10,2)");
        });

        builder.Entity<BusinessHours>(entity =>
        {
            entity.ToTable("BusinessHours");
            entity.HasKey(e => e.Id);
        });

        builder.Entity<DeliveryArea>(entity =>
        {
            entity.ToTable("DeliveryAreas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeliveryFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.MinOrder).HasColumnType("decimal(10,2)");
        });

        builder.Entity<DeliveryPerson>(entity =>
        {
            entity.ToTable("DeliveryPersons");
            entity.HasKey(e => e.Id);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Phone);
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(10,2)");
        });
    }
}
