// Data/VentasDbContext.cs
using Microsoft.EntityFrameworkCore;
using eromodeshopp.Models;

namespace eromodeshopp.Data
{
    public class VentasDbContext : DbContext
    {
        public VentasDbContext(DbContextOptions<VentasDbContext> options) : base(options) { }

        public DbSet<HechoVentas> HechoVentas { get; set; } = null!;
    }
}