using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    [Table("HechoVentas")]
    public class HechoVentas
    {
        [Key]
        public int IdVenta { get; set; }

        public int IdOrden { get; set; }
        public int IdUsuario { get; set; }
        public int IdProducto { get; set; }

        [StringLength(200)]
        public string? NombreProducto { get; set; }

        [StringLength(100)]
        public string? Marca { get; set; }

        [StringLength(10)]
        public string? Talla { get; set; }

        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalProducto { get; set; } // Calculado en SQL Server

        public DateTime FechaOrden { get; set; }

        [StringLength(50)]
        public string? MetodoPago { get; set; }

        [StringLength(250)]
        public string? EmailUsuario { get; set; }
    }
}