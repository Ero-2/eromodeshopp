using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    [Table("Facturas")]
    public class Factura
    {
        [Key]
        [Column("IdFactura")]
        public int IdFactura { get; set; }

        [Column("IdOrden")]
        public int IdOrden { get; set; }

        [Column("NumeroFactura")]
        public string NumeroFactura { get; set; } = string.Empty;

        [Column("FechaEmision")]
        public DateTime FechaEmision { get; set; } = DateTime.Today;

        [Column("RUC_DNI_Cliente")]
        public string RUC_DNI_Cliente { get; set; } = string.Empty;

        [Column("NombreCliente")]
        public string NombreCliente { get; set; } = string.Empty;

        [Column("DireccionCliente")]
        public string DireccionCliente { get; set; } = string.Empty;

        [Column("TotalBruto")]
        public decimal TotalBruto { get; set; }

        [Column("Impuestos")]
        public decimal Impuestos { get; set; }

        [Column("TotalNeto")]
        public decimal TotalNeto { get; set; }

        [Column("Estado")]
        public string Estado { get; set; } = "emitida";

        [Column("PDF_URL")]
        public string? PDF_URL { get; set; }

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("FechaModificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

        [Column("UsuarioCreacion")]
        public string? UsuarioCreacion { get; set; }

        [Column("UsuarioModificacion")]
        public string? UsuarioModificacion { get; set; }

        // Relación opcional
        [ForeignKey("IdOrden")]
        public virtual Orden? Orden { get; set; }
    }
}