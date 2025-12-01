using System.ComponentModel.DataAnnotations;


namespace eromodeshopp.Models
{
    public class Talla
    {
        [Key]
        public int IdTalla { get; set; }

        [Required(ErrorMessage = "El nombre de la talla es obligatorio")]
        [StringLength(10)]
        public string NombreTalla { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public virtual ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();
    }
}
