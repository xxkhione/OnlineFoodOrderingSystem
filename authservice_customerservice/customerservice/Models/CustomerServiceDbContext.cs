using Microsoft.EntityFrameworkCore;
public class CustomerServiceDbContext(DbContextOptions<CustomerServiceDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.CustomerGuid);
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.CreatedDate).HasDefaultValueSql("now()");
        });
    }
}
