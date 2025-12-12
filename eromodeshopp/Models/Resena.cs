using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    [Table("Resenas")]
    public class Resena
    {
        [Key]
        public int IdResena { get; set; }

        [Required]
        public int IdProducto { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        public string? Comentario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public bool Aprobada { get; set; } = false;

        public string? RespuestaAdmin { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        // Relaciones (opcional, si quieres usar EF Core para cargar datos relacionados)
        [ForeignKey("IdProducto")]
        public virtual Producto? Producto { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}