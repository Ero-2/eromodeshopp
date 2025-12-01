using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ← Solo usuarios autenticados pueden usar este controlador
    public class CarritoController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;

        public CarritoController(EromodeshopDbContext context)
        {
            _context = context;
        }

        // GET: api/carrito
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarritoConProducto>>> GetCarrito()
        {
            var userId = GetUserIdFromToken();
            if (userId == 0)
                return Unauthorized();

            var carrito = await _context.Carrito
                .Where(c => c.IdUsuario == userId)
                .Include(c => c.Inventario)
                    .ThenInclude(i => i.Producto)
                .Include(c => c.Inventario)
                    .ThenInclude(i => i.Talla)
                .Select(c => new CarritoConProducto
                {
                    IdCarrito = c.IdCarrito,
                    IdProducto = c.Inventario.IdProducto,
                    NombreProducto = c.Inventario.Producto.Nombre,
                    Talla = c.Inventario.Talla.NombreTalla,
                    Cantidad = c.Cantidad,
                    PrecioUnitario = c.Inventario.Producto.Precio,
                    StockDisponible = c.Inventario.Stock
                })
                .ToListAsync();

            return Ok(carrito);
        }

        // POST: api/carrito
        [HttpPost]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartModel model)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0)
                return Unauthorized();

            // Validar inventario
            var inventario = await _context.Inventario
                .FirstOrDefaultAsync(i => i.IdInventario == model.IdInventario);
            if (inventario == null)
                return BadRequest("El producto no existe.");

            if (inventario.Stock < model.Cantidad)
                return BadRequest("No hay suficiente stock disponible.");

            // Verificar si ya está en el carrito
            var itemExistente = await _context.Carrito
                .FirstOrDefaultAsync(c => c.IdUsuario == userId && c.IdInventario == model.IdInventario);

            if (itemExistente != null)
            {
                // Si ya está, sumar cantidad
                if (itemExistente.Cantidad + model.Cantidad > inventario.Stock)
                    return BadRequest("No hay suficiente stock para la cantidad deseada.");

                itemExistente.Cantidad += model.Cantidad;
                itemExistente.FechaModificacion = DateTime.UtcNow;
                _context.Carrito.Update(itemExistente);
            }
            else
            {
                // Si no, crear nuevo
                var nuevoItem = new Carrito
                {
                    IdUsuario = userId,
                    IdInventario = model.IdInventario,
                    Cantidad = model.Cantidad,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
                };
                _context.Carrito.Add(nuevoItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Producto agregado al carrito." });
        }

        // DELETE: api/carrito/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0)
                return Unauthorized();

            var item = await _context.Carrito
                .FirstOrDefaultAsync(c => c.IdCarrito == id && c.IdUsuario == userId);

            if (item == null)
                return NotFound("Producto no encontrado en tu carrito.");

            _context.Carrito.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Producto eliminado del carrito." });
        }

        // Método auxiliar para obtener el ID del usuario del token JWT
        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("nameid")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }

    // Modelos DTO para la API
    public class AddToCartModel
    {
        public int IdInventario { get; set; }
        public int Cantidad { get; set; } = 1;
    }

    public class CarritoConProducto
    {
        public int IdCarrito { get; set; }
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int StockDisponible { get; set; }
    }
}