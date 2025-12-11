using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdenController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;
        private readonly VentasDbContext _ventasContext;

        public OrdenController(EromodeshopDbContext context, VentasDbContext ventasContext)
        {
            _context = context;
            _ventasContext = ventasContext;
        }

        // POST: api/orden/checkout
        [HttpPost("checkout")]
        public async Task<ActionResult<OrdenResponse>> Checkout([FromBody] CheckoutRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Token inválido o usuario no encontrado." });
            }

            if (request.Productos == null || !request.Productos.Any())
            {
                return BadRequest(new { error = "El carrito está vacío." });
            }

            decimal total = 0;
            var detallesOrden = new List<DetalleOrden>();

            foreach (var producto in request.Productos)
            {
                var inventario = await _context.Inventario
                    .Include(i => i.Producto)
                        .ThenInclude(p => p.Marca)
                    .Include(i => i.Talla)
                    .FirstOrDefaultAsync(i =>
                        i.IdProducto == producto.IdProducto &&
                        i.Talla.NombreTalla == producto.Talla);

                if (inventario == null)
                {
                    return BadRequest(new
                    {
                        error = $"Producto '{producto.Nombre}' en talla '{producto.Talla}' no encontrado."
                    });
                }

                if (inventario.Stock < producto.Cantidad)
                {
                    return BadRequest(new
                    {
                        error = $"No hay suficiente stock para '{inventario.Producto.Nombre}' en talla '{inventario.Talla.NombreTalla}'. Stock disponible: {inventario.Stock}"
                    });
                }

                total += inventario.Producto.Precio * producto.Cantidad;

                detallesOrden.Add(new DetalleOrden
                {
                    IdInventario = inventario.IdInventario,
                    Cantidad = producto.Cantidad,
                    PrecioUnitario = inventario.Producto.Precio,
                    Status = "pendiente",
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow,
                    UsuarioCreacion = User.Identity?.Name ?? "sistema"
                });

                inventario.Stock -= producto.Cantidad;
                inventario.FechaModificacion = DateTime.UtcNow;
                _context.Entry(inventario).State = EntityState.Modified;
            }

            var orden = new Orden
            {
                IdUsuario = userId,
                Total = total,
                FechaOrden = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                DireccionEnvio = request.DireccionEnvio,
                MetodoPago = request.MetodoPago,
                status = "pendiente",
                referencia = null
            };

            _context.Orden.Add(orden);
            await _context.SaveChangesAsync();

            foreach (var detalle in detallesOrden)
            {
                detalle.IdOrden = orden.IdOrden;
            }

            _context.DetalleOrden.AddRange(detallesOrden);
            await _context.SaveChangesAsync();

            // 🔥 Sincronizar con base de datos de ventas (SQL Server)
            await SincronizarConVentasAsync(orden, detallesOrden);

            // ⭐ MANEJO ESPECIAL PARA TARJETA
            if (request.MetodoPago.ToLower() == "tarjeta")
            {
                string referencia = GenerateReference();

                orden.referencia = referencia;
                orden.status = "procesado";
                _context.Orden.Update(orden);

                var detallePago = new DetallePago
                {
                    idOrden = orden.IdOrden,
                    formaPago = request.MetodoPago,
                    status = "completado",
                    referencia = referencia,
                    cardbrand = GetCardBrand(request.numeroTarjeta),
                    cardlast4 = GetLastFourDigits(request.numeroTarjeta)
                };

                _context.DetallePago.Add(detallePago);
                await _context.SaveChangesAsync();
            }

            // Limpiar carrito
            var carritoItems = await _context.Carrito
                .Where(c => c.IdUsuario == userId)
                .ToListAsync();
            if (carritoItems.Any())
            {
                _context.Carrito.RemoveRange(carritoItems);
                await _context.SaveChangesAsync();
            }

            var ordenResponse = new OrdenResponse
            {
                IdOrden = orden.IdOrden,
                Total = orden.Total,
                FechaOrden = orden.FechaOrden,
                Productos = detallesOrden.Select(d => new ProductoEnOrden
                {
                    Nombre = _context.Inventario
                        .Include(i => i.Producto)
                        .First(i => i.IdInventario == d.IdInventario)
                        .Producto.Nombre,
                    Talla = _context.Inventario
                        .Include(i => i.Talla)
                        .First(i => i.IdInventario == d.IdInventario)
                        .Talla.NombreTalla,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList()
            };

            return Ok(ordenResponse);
        }

        // GET: api/orden
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrdenResumen>>> GetOrdenes()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Token inválido." });
            }

            var ordenes = await _context.Orden
                .Where(o => o.IdUsuario == userId)
                .Select(o => new OrdenResumen
                {
                    IdOrden = o.IdOrden,
                    Total = o.Total,
                    FechaOrden = o.FechaOrden
                })
                .ToListAsync();

            return Ok(ordenes);
        }

        // ✅ GET: api/orden/{id}/detalles - Con estados enriquecidos y porcentajes correctos
        [HttpGet("{id}/detalles")]
        public async Task<ActionResult> GetDetallesOrden(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Token inválido." });
            }

            var orden = await _context.Orden
                .FirstOrDefaultAsync(o => o.IdOrden == id && o.IdUsuario == userId);

            if (orden == null)
            {
                return NotFound(new { error = "Orden no encontrada" });
            }

            var detalles = await _context.DetalleOrden
                .Include(d => d.Inventario)
                    .ThenInclude(i => i.Producto)
                .Include(d => d.Inventario)
                    .ThenInclude(i => i.Talla)
                .Where(d => d.IdOrden == id)
                .ToListAsync();

            var detallesConEstado = detalles.Select(d => new
            {
                idDetalleOrden = d.IdDetalleOrden,
                producto = d.Inventario.Producto.Nombre,
                talla = d.Inventario.Talla.NombreTalla,
                cantidad = d.Cantidad,
                precioUnitario = d.PrecioUnitario,
                subtotal = d.Subtotal,
                status = d.Status,
                nombreEstado = d.Status switch
                {
                    "pendiente" => "Pendiente",
                    "preparando" => "En preparación",
                    "revisado" => "Revisado",
                    "liberado" => "Liberado",
                    "entregado" => "Entregado",
                    "procesado" => "Procesado",
                    "completado" => "Completado",
                    "cancelado" => "Cancelado",
                    _ => "Desconocido"
                },
                colorEstado = d.Status switch
                {
                    "pendiente" => "#FFA500", // Naranja
                    "preparando" => "#3B82F6", // Azul
                    "revisado" => "#9333EA",   // Morado
                    "liberado" => "#10B981",   // Verde
                    "entregado" => "#10B981",  // Verde
                    "procesado" => "#4F46E5",  // Azul Indigo
                    "completado" => "#10B981", // Verde
                    "cancelado" => "#EF4444",  // Rojo
                    _ => "#9CA3AF"             // Gris
                },
                iconoEstado = d.Status switch
                {
                    "pendiente" => "Clock",
                    "preparando" => "Package",
                    "revisado" => "Eye",
                    "liberado" => "Truck",
                    "entregado" => "CheckCircle",
                    "procesado" => "CreditCard",
                    "completado" => "CheckCircle",
                    "cancelado" => "XCircle",
                    _ => "HelpCircle"
                },
                progreso = d.Status switch
                {
                    "pendiente" => 0,
                    "preparando" => 25,  // ✅ Corregido a 25%
                    "revisado" => 50,    // ✅ Corregido a 50%
                    "liberado" => 75,    // ✅ Corregido a 75%
                    "entregado" => 100,  // ✅ Corregido a 100%
                    "procesado" => 50,
                    "completado" => 100,
                    "cancelado" => 0,
                    _ => 0
                }
            }).ToList();

            return Ok(detallesConEstado);
        }

        // 🔁 MÉTODOS AUXILIARES
        private async Task SincronizarConVentasAsync(Orden orden, List<DetalleOrden> detalles)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == orden.IdUsuario);

                foreach (var detalle in detalles)
                {
                    var inventario = await _context.Inventario
                        .Include(i => i.Producto)
                            .ThenInclude(p => p.Marca)
                        .Include(i => i.Talla)
                        .FirstAsync(i => i.IdInventario == detalle.IdInventario);

                    var hecho = new HechoVentas
                    {
                        IdOrden = orden.IdOrden,
                        IdUsuario = orden.IdUsuario,
                        IdProducto = inventario.IdProducto,
                        NombreProducto = inventario.Producto.Nombre,
                        Marca = inventario.Producto.Marca.Nombre,
                        Talla = inventario.Talla.NombreTalla,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario,
                        FechaOrden = orden.FechaOrden,
                        MetodoPago = orden.MetodoPago,
                        EmailUsuario = usuario?.Email ?? "desconocido@example.com"
                    };

                    _ventasContext.HechoVentas.Add(hecho);
                }

                await _ventasContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR VENTAS] {ex.Message}");
            }
        }

        private string GenerateReference()
        {
            return $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
        }

        private string GetCardBrand(string numeroTarjeta)
        {
            if (string.IsNullOrEmpty(numeroTarjeta)) return "Desconocida";
            string clean = numeroTarjeta.Replace(" ", "").Replace("-", "");
            if (clean.StartsWith("4")) return "Visa";
            if (clean.StartsWith("5") && clean.Length > 1 && "12345".Contains(clean[1])) return "Mastercard";
            if (clean.StartsWith("34") || clean.StartsWith("37")) return "American Express";
            if (clean.StartsWith("6011")) return "Discover";
            return "Desconocida";
        }

        private string GetLastFourDigits(string numeroTarjeta)
        {
            if (string.IsNullOrEmpty(numeroTarjeta)) return "";
            string clean = numeroTarjeta.Replace(" ", "").Replace("-", "");
            return clean.Length >= 4 ? clean.Substring(clean.Length - 4) : "";
        }

        // ⭐ DTOs
        public class CheckoutRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DireccionEnvio { get; set; } = string.Empty;
            public string MetodoPago { get; set; } = string.Empty;
            public string? numeroTarjeta { get; set; }
            public string? fechaExpiracion { get; set; }
            public string? cvv { get; set; }
            public List<ProductoCheckout> Productos { get; set; } = new();
        }

        public class ProductoCheckout
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Talla { get; set; } = string.Empty;
            public int Cantidad { get; set; }
        }

        public class OrdenResponse
        {
            public int IdOrden { get; set; }
            public decimal Total { get; set; }
            public DateTime FechaOrden { get; set; }
            public List<ProductoEnOrden> Productos { get; set; } = new();
        }

        public class ProductoEnOrden
        {
            public string Nombre { get; set; } = string.Empty;
            public string Talla { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class OrdenResumen
        {
            public int IdOrden { get; set; }
            public decimal Total { get; set; }
            public DateTime FechaOrden { get; set; }
        }
    }
}