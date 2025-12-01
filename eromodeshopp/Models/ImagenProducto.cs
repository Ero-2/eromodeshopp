using eromodeshopp.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    [Table("ImagenesProducto")]
    public class ImagenProducto
    {
        [Key]
        [Column("IdImagen")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdImagen { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio")]
        [ForeignKey("Producto")]
        [Column("IdProducto")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La URL es obligatoria")]
        [Column("Url")] // ⬅️ Mapea a la columna "Url" en la BD
        public string UrlImagen { get; set; } = string.Empty;

        [Column("EsPrincipal")]
        public bool EsPrincipal { get; set; } = false;

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("FechaModificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

        [Column("UsuarioCreacion")]
        public string? UsuarioCreacion { get; set; }

        [Column("UsuarioModificacion")]
        public string? UsuarioModificacion { get; set; }

        // Relación con Producto
        public virtual Producto Producto { get; set; } = null!;
    }
}