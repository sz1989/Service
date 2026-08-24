using Service.Model;

namespace Service.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();

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
    }
}
