
using eromodeshopp.Models;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Data
{
    public class EromodeshopDbContext : DbContext
    {
        public EromodeshopDbContext(DbContextOptions<EromodeshopDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Talla> Tallas { get; set; }
        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<Carrito> Carrito { get; set; }
        public DbSet<Orden> Orden { get; set; }
        public DbSet<DetalleOrden> DetalleOrden { get; set; }
        public DbSet<ImagenProducto> ImagenesProducto { get; set; }
        // 👇 Añade este DbSet para HechoVentas
        public DbSet<HechoVentas> HechoVentas { get; set; } = null!;

        public DbSet<DetallePago> DetallePago { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relaciones
            modelBuilder.Entity<Carrito>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Carritos)
                .HasForeignKey(c => c.IdUsuario);

            modelBuilder.Entity<Carrito>()
                .HasOne(c => c.Inventario)
                .WithMany(i => i.Carritos)
                .HasForeignKey(c => c.IdInventario);

            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.Producto)
                .WithMany(p => p.Inventarios)
                .HasForeignKey(i => i.IdProducto);

            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.Talla)
                .WithMany(t => t.Inventarios)
                .HasForeignKey(i => i.IdTalla);

            modelBuilder.Entity<DetalleOrden>()
                .HasOne(d => d.Orden)
                .WithMany(o => o.DetallesOrden)
                .HasForeignKey(d => d.IdOrden);

            modelBuilder.Entity<DetalleOrden>()
                .HasOne(d => d.Inventario)
                .WithMany(i => i.DetallesOrden)
                .HasForeignKey(d => d.IdInventario);

            modelBuilder.Entity<Orden>()
                .HasOne(o => o.Usuario)
                .WithMany(u => u.Ordenes)
                .HasForeignKey(o => o.IdUsuario);

            

            // Nombres de tablas
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Marca>().ToTable("Marcas");
            modelBuilder.Entity<Producto>().ToTable("Productos");
            modelBuilder.Entity<Talla>().ToTable("Tallas");
            modelBuilder.Entity<Inventario>().ToTable("Inventario");
            modelBuilder.Entity<Carrito>().ToTable("Carrito");
            modelBuilder.Entity<Orden>().ToTable("Ordenes");
            modelBuilder.Entity<DetalleOrden>().ToTable("DetalleOrden");


            base.OnModelCreating(modelBuilder);
        }
    }
}
