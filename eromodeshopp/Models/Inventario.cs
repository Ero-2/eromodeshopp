using System.ComponentModel.DataAnnotations;

namespace eromodeshopp.Models
{
    public class Inventario
    {
        [Key]
        public int IdInventario { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La talla es obligatoria")]
        public int IdTalla { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public virtual Producto Producto { get; set; } = null!;
        public virtual Talla Talla { get; set; } = null!;
        public virtual ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();
        public virtual ICollection<DetalleOrden> DetallesOrden { get; set; } = new List<DetalleOrden>();
    }
}

