using System.ComponentModel.DataAnnotations;


namespace eromodeshopp.Models
{
    public class Marca
    {
        [Key]
        public int IdMarca { get; set; }

        [Required(ErrorMessage = "El nombre de la marca es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();


    }
}
