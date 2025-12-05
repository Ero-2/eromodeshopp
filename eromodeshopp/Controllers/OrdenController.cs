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
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
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
                MetodoPago = request.MetodoPago
            };

            _context.Orden.Add(orden);
            await _context.SaveChangesAsync();

            foreach (var detalle in detallesOrden)
            {
                detalle.IdOrden = orden.IdOrden;
            }

            _context.DetalleOrden.AddRange(detallesOrden);
            await _context.SaveChangesAsync();

            // 🔥 SINCRONIZAR CON BASE DE DATOS DE VENTAS (SQL SERVER)
            await SincronizarConVentasAsync(orden, detallesOrden);

            // Limpiar carrito (opcional)
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

        // 🔁 MÉTODO PRIVADO: Sincroniza la venta con SQL Server
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
                // En desarrollo: imprimir error. En producción: usar ILogger
                Console.WriteLine($"[ERROR VENTAS] {ex.Message}");
                // No lanzamos excepción: no queremos romper el checkout
            }
        }

        // ⭐ DTOs
        public class CheckoutRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DireccionEnvio { get; set; } = string.Empty;
            public string MetodoPago { get; set; } = string.Empty;
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