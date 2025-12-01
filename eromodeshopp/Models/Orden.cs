using System.ComponentModel.DataAnnotations;

namespace eromodeshopp.Models
{
    public class Orden
    {
        [Key]
        public int IdOrden { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public int IdUsuario { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El total no puede ser negativo")]
        public decimal Total { get; set; }

        [Required]
        public string DireccionEnvio { get; set; } = string.Empty;

        [Required]
        public string MetodoPago { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; } = DateTime.UtcNow;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
        public virtual ICollection<DetalleOrden> DetallesOrden { get; set; } = new List<DetalleOrden>();
    }
}
