using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    [Table("DetallePago")]
    public class DetallePago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idDetallePago { get; set; }

        [Required]
        public int idOrden { get; set; }

        [Required]
        [StringLength(50)]
        public string formaPago { get; set; } = string.Empty; // "tarjeta", "oxxo", "transferencia"

        [StringLength(20)]
        public string status { get; set; } = "pendiente"; // "pendiente", "procesado", "cancelado"

        [Required]
        [StringLength(50)]
        public string referencia { get; set; } = string.Empty; // Número de referencia único

        public DateTime fechaCreacion { get; set; } = DateTime.UtcNow;

        // ✅ Nuevas columnas para pagos con tarjeta
        [StringLength(20)]
        public string? cardbrand { get; set; } // Ej: "Visa", "Mastercard"

        [StringLength(4)]
        public string? cardlast4 { get; set; } // Últimos 4 dígitos de la tarjeta

        // Relación con Orden
        [ForeignKey("idOrden")]
        public virtual Orden orden { get; set; }
    }
}