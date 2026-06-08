using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using POS.Models;

namespace POS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Area> Area { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<EmployeeArea> EmployeeArea { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<VehicleIn> VehicleIn { get; set; }
        public DbSet<VehicleInDetail> VehicleInDetail { get; set; }
        public DbSet<Setting> Setting { get; set; }
        public DbSet<VehicleOut> VehicleOut { get; set; }
        public DbSet<Vehicle> Vehicle { get; set; }
        public DbSet<Production> Production { get; set; }
        public DbSet<RawDetail> RawDetails { get; set; }
        public DbSet<FinishDetail> FinishDetails { get; set; }



    }
}
