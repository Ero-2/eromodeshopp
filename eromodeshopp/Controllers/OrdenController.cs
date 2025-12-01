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

        public OrdenController(EromodeshopDbContext context)
        {
            _context = context;
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

            // ⭐ Validar que vengan productos en el request
            if (request.Productos == null || !request.Productos.Any())
            {
                return BadRequest(new { error = "El carrito está vacío." });
            }

            // ⭐ Validar stock y obtener datos de cada producto
            decimal total = 0;
            var detallesOrden = new List<DetalleOrden>();

            foreach (var producto in request.Productos)
            {
                // Buscar el inventario por IdProducto + Talla
                var inventario = await _context.Inventario
                    .Include(i => i.Producto)
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

                // Calcular subtotal
                total += inventario.Producto.Precio * producto.Cantidad;

                // Preparar detalle de orden (lo guardamos después)
                detallesOrden.Add(new DetalleOrden
                {
                    IdInventario = inventario.IdInventario,
                    Cantidad = producto.Cantidad,
                    PrecioUnitario = inventario.Producto.Precio,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow,
                    Inventario = inventario // Para la respuesta
                });

                // Reducir stock
                inventario.Stock -= producto.Cantidad;
                inventario.FechaModificacion = DateTime.UtcNow;
                _context.Entry(inventario).State = EntityState.Modified;
            }

            // Crear la orden
            var orden = new Orden
            {
                IdUsuario = userId,
                Total = total,
                FechaOrden = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                DireccionEnvio = request.DireccionEnvio,
                MetodoPago = request.MetodoPago
            };

            _context.Orden.Add(orden);
            await _context.SaveChangesAsync(); // Guardar para obtener IdOrden

            // Asignar IdOrden a los detalles y guardar
            foreach (var detalle in detallesOrden)
            {
                detalle.IdOrden = orden.IdOrden;
            }

            _context.DetalleOrden.AddRange(detallesOrden);
            await _context.SaveChangesAsync();

            // Limpiar carrito de la BD si existe (opcional)
            var carritoItems = await _context.Carrito
                .Where(c => c.IdUsuario == userId)
                .ToListAsync();
            if (carritoItems.Any())
            {
                _context.Carrito.RemoveRange(carritoItems);
                await _context.SaveChangesAsync();
            }

            // Devolver respuesta
            var ordenResponse = new OrdenResponse
            {
                IdOrden = orden.IdOrden,
                Total = orden.Total,
                FechaOrden = orden.FechaOrden,
                Productos = detallesOrden.Select(d => new ProductoEnOrden
                {
                    Nombre = d.Inventario.Producto.Nombre,
                    Talla = d.Inventario.Talla.NombreTalla,
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

        // ⭐ DTOs actualizados
        public class CheckoutRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DireccionEnvio { get; set; } = string.Empty;
            public string MetodoPago { get; set; } = string.Empty;
            public List<ProductoCheckout> Productos { get; set; } = new(); // ⭐ NUEVO
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