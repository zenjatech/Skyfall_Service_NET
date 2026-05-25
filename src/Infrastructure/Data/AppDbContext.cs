using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<CafeTable> Tables => Set<CafeTable>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ItemVariant> ItemVariants => Set<ItemVariant>();
    public DbSet<ItemAddon> ItemAddons => Set<ItemAddon>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<KOT> KOTs => Set<KOT>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(100).IsRequired();
            e.Property(t => t.Plan).HasMaxLength(50).IsRequired();
            e.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<Staff>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
            e.Property(s => s.Email).HasMaxLength(256).IsRequired();
            e.Property(s => s.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(s => s.Role).HasMaxLength(50).IsRequired();
            e.HasIndex(s => new { s.TenantId, s.Email }).IsUnique();
        });

        modelBuilder.Entity<CafeTable>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Status).HasMaxLength(50).IsRequired();
            e.Property(t => t.QrCodeUrl).HasMaxLength(2048);
            e.HasIndex(t => new { t.TenantId, t.TableNumber }).IsUnique();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Icon).HasMaxLength(100);
            e.HasMany(c => c.MenuItems).WithOne(m => m.Category).HasForeignKey(m => m.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuItem>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(200).IsRequired();
            e.Property(m => m.Description).HasMaxLength(1000);
            e.Property(m => m.BasePrice).HasColumnType("decimal(10,2)");
            e.Property(m => m.ImageUrl).HasMaxLength(2048);
            e.HasMany(m => m.Variants).WithOne(v => v.Item).HasForeignKey(v => v.ItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(m => m.Addons).WithOne(a => a.Item).HasForeignKey(a => a.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemVariant>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Name).HasMaxLength(100).IsRequired();
            e.Property(v => v.PriceModifier).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<ItemAddon>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(100).IsRequired();
            e.Property(a => a.ExtraPrice).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Phone).HasMaxLength(20).IsRequired();
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.TotalSpent).HasColumnType("decimal(12,2)");
            e.HasIndex(c => new { c.TenantId, c.Phone }).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasMaxLength(50).IsRequired();
            e.Property(o => o.OrderType).HasMaxLength(50).IsRequired();
            e.Property(o => o.Subtotal).HasColumnType("decimal(12,2)");
            e.Property(o => o.TaxAmount).HasColumnType("decimal(12,2)");
            e.Property(o => o.DiscountAmount).HasColumnType("decimal(12,2)");
            e.Property(o => o.TotalAmount).HasColumnType("decimal(12,2)");
            e.Property(o => o.SpecialInstructions).HasMaxLength(1000);
            e.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(o => o.KOTs).WithOne(k => k.Order).HasForeignKey(k => k.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.Invoice).WithOne(i => i.Order).HasForeignKey<Invoice>(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(o => o.Payments).WithOne(p => p.Order).HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.Table).WithMany().HasForeignKey(o => o.TableId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(o => o.PlacedByStaff).WithMany().HasForeignKey(o => o.PlacedByStaffId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(10,2)");
            e.Property(i => i.AddonsJson).HasMaxLength(4000);
            e.Property(i => i.SpecialInstructions).HasMaxLength(500);
            e.Property(i => i.ItemStatus).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<KOT>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(k => new { k.TenantId, k.KotNumber }).IsUnique();
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(i => i.PdfUrl).HasMaxLength(2048);
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.HasIndex(i => i.OrderId).IsUnique();
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Mode).HasMaxLength(50).IsRequired();
            e.Property(p => p.Amount).HasColumnType("decimal(12,2)");
            e.Property(p => p.Status).HasMaxLength(50).IsRequired();
            e.Property(p => p.RazorpayOrderId).HasMaxLength(100);
            e.Property(p => p.RazorpayPaymentId).HasMaxLength(100);
        });
    }
}
