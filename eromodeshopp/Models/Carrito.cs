using System.ComponentModel.DataAnnotations;

namespace eromodeshopp.Models
{
    public class Carrito
    {
        [Key]
        public int IdCarrito { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El inventario es obligatorio")]
        public int IdInventario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; } = 1;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
        public virtual Inventario Inventario { get; set; } = null!;
    }
}
