// eromodeshopp.Models / DetalleOrden.cs
using System.ComponentModel.DataAnnotations;

namespace eromodeshopp.Models
{
    public class DetalleOrden
    {
        [Key]
        public int IdDetalleOrden { get; set; }

        [Required(ErrorMessage = "La orden es obligatoria")]
        public int IdOrden { get; set; }

        [Required(ErrorMessage = "El inventario es obligatorio")]
        public int IdInventario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo")]
        public decimal PrecioUnitario { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public virtual Orden Orden { get; set; } = null!;
        public virtual Inventario Inventario { get; set; } = null!;
    }
}