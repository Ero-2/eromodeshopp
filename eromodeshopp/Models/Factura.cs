using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    [Table("Facturas")]
    public class Factura
    {
        [Key]
        public int IdFactura { get; set; }

        [Required]
        public int IdOrden { get; set; }

        [Required]
        [StringLength(50)]
        [Column(TypeName = "character varying(50)")]
        public string NumeroFactura { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "date")]
        public DateTime FechaEmision { get; set; }

        [Required]
        [StringLength(20)]
        [Column("RUC_DNI_Cliente", TypeName = "character varying(20)")]
        public string RUC_DNI_Cliente { get; set; } = string.Empty;

        [Required]
        [StringLength(255)] // Cambiar de 200 a 255 para coincidir con BD
        [Column("NombreCliente", TypeName = "character varying(255)")]
        public string NombreCliente { get; set; } = string.Empty;

        [Required]
        [Column("DireccionCliente", TypeName = "text")]
        public string DireccionCliente { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "numeric(10,2)")] // Cambiar de decimal(18,2) a numeric(10,2)
        public decimal TotalBruto { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Impuestos { get; set; }

        [Required]
        [Column(TypeName = "numeric(10,2)")]
        public decimal TotalNeto { get; set; }

        [Required]
        [StringLength(20)]
        [Column(TypeName = "character varying(20)")]
        public string Estado { get; set; } = "emitida";

        [Column("PDF_URL", TypeName = "text")]
        public string? PDF_URL { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime FechaCreacion { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime FechaModificacion { get; set; }

        [Column(TypeName = "text")]
        public string UsuarioCreacion { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string UsuarioModificacion { get; set; } = string.Empty;

        // Relaciones
        [ForeignKey("IdOrden")]
        public virtual Orden? Orden { get; set; }
    }
}