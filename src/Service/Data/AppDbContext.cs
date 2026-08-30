using Service.Model;

namespace Service.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();

    public DbSet<InventoryItem> Inventory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("person");

            entity.Property((p => p.Id)).HasColumnName("id");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name).HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.DateOfBirth).HasColumnName("date_of_birth")
                .IsRequired();

            entity.Property(p => p.ManagerId).HasColumnName("manager_id");

            entity.Property(p => p.Salary).HasColumnName("salary")
                .IsRequired()
                .HasColumnType("numeric(10, 2)");
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("inventory"); // matches your Postgres table name
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");

            // 1. Tell EF Core to map this complex object to a JSON column
            entity.OwnsOne(e => e.Attributes, builder =>
            {
                builder.ToJson("attributes"); // matches your Postgres JSONB column name
                
                // 2. Optional: Map C# property names to specific JSON key casings if they don't match
                builder.OwnsOne(a => a.Specs, specsBuilder =>
                {
                    specsBuilder.Property(s => s.RamGb).HasJsonPropertyName("ram_gb");
                    specsBuilder.Property(s => s.StorageGb).HasJsonPropertyName("storage_gb");
                });
            });
        });
    }
}
