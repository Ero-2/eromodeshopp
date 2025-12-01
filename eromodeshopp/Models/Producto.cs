using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace eromodeshopp.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La marca es obligatoria")]
        public int IdMarca { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal Precio { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        [ForeignKey("IdMarca")]
        public virtual Marca Marca { get; set; } = null!;
        public virtual ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();

        // ✅ RELACIÓN CON IMÁGENES — ¡ESTO FALTABA!
        public virtual ICollection<ImagenProducto> ImagenesProducto { get; set; } = new List<ImagenProducto>();
    }
}

