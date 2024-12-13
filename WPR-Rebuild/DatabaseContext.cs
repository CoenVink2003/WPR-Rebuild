using Microsoft.EntityFrameworkCore;
public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
    
    public DbSet<Vehicle> Vehicles { get; set; }
}
