using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eromodeshopp.Models
{
    public class DetalleOrden
    {
        [Key]
        public int IdDetalleOrden { get; set; }

        [Required(ErrorMessage = "La orden es obligatoria")]
        public int IdOrden { get; set; }

        [Required(ErrorMessage = "El inventario es obligatorio")]
        public int IdInventario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo")]
        public decimal PrecioUnitario { get; set; }

        // ✅ NUEVA COLUMNA: Estado del pedido — Mapeada explícitamente a "status"
        [Required]
        [StringLength(50)]
        [Column("status")] // ⭐ Corrección: mapea a la columna "status" en la DB
        public string Status { get; set; } = "pendiente";

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }

        // Relaciones
        public virtual Orden Orden { get; set; } = null!;
        public virtual Inventario Inventario { get; set; } = null!;


        // ✅ PIPELINE DE ESTADOS (igual que en PHP/Laravel)
        public static readonly Dictionary<string, EstadoConfig> PIPELINE = new()
        {
            { "pendiente", new EstadoConfig
                {
                    Nombre = "Pendiente",
                    Color = "bg-yellow-500 text-white",
                    Icono = "far fa-clock",
                    Orden = 1,
                    Progreso = 0
                }
            },
            { "preparando", new EstadoConfig
                {
                    Nombre = "En preparación",
                    Color = "bg-blue-500 text-white",
                    Icono = "fas fa-box-open",
                    Orden = 2,
                    Progreso = 25
                }
            },
            { "revisado", new EstadoConfig
                {
                    Nombre = "Revisado",
                    Color = "bg-purple-500 text-white",
                    Icono = "fas fa-check-double",
                    Orden = 3,
                    Progreso = 50
                }
            },
            { "liberado", new EstadoConfig
                {
                    Nombre = "Liberado",
                    Color = "bg-green-500 text-white",
                    Icono = "fas fa-check-circle",
                    Orden = 4,
                    Progreso = 75
                }
            },
            { "entregado", new EstadoConfig
                {
                    Nombre = "Entregado",
                    Color = "bg-teal-600 text-white",
                    Icono = "fas fa-truck",
                    Orden = 5,
                    Progreso = 100
                }
            },
            { "cancelado", new EstadoConfig
                {
                    Nombre = "Cancelado",
                    Color = "bg-red-500 text-white",
                    Icono = "fas fa-times-circle",
                    Orden = 0,
                    Progreso = 0
                }
            }
        };

        // ✅ Propiedades calculadas (NotMapped = no se guardan en DB)
        [NotMapped]
        public string NombreEstado => PIPELINE.ContainsKey(Status)
            ? PIPELINE[Status].Nombre
            : Status;

        [NotMapped]
        public string ColorEstado => PIPELINE.ContainsKey(Status)
            ? PIPELINE[Status].Color
            : "bg-gray-500 text-white";

        [NotMapped]
        public string IconoEstado => PIPELINE.ContainsKey(Status)
            ? PIPELINE[Status].Icono
            : "fas fa-circle";

        [NotMapped]
        public int Progreso => PIPELINE.ContainsKey(Status)
            ? PIPELINE[Status].Progreso
            : 0;

        [NotMapped]
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }

    // ✅ Clase auxiliar para configuración de estados
    public class EstadoConfig
    {
        public string Nombre { get; set; } = "";
        public string Color { get; set; } = "";
        public string Icono { get; set; } = "";
        public int Orden { get; set; }
        public int Progreso { get; set; }
    }
}