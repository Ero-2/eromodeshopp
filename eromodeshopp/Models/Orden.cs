using System.ComponentModel.DataAnnotations;
using eromodeshopp.Models;

public class Orden
{
    [Key]
    public int IdOrden { get; set; }

    [Required]
    public int IdUsuario { get; set; }

    [Range(0, double.MaxValue)]
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

    [StringLength(50)]
    public string? referencia { get; set; }

    // 👇 NUEVA PROPIEDAD
    [StringLength(20)]
    public string status { get; set; } = "pendiente"; // Valores: 'pendiente', 'procesando_pago', 'completado', 'cancelado'

    public virtual ICollection<DetalleOrden> DetallesOrden { get; set; } = new List<DetalleOrden>();
    public virtual ICollection<DetallePago> DetallePagos { get; set; } = new List<DetallePago>();
}