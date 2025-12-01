using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;

        public InventarioController(EromodeshopDbContext context)
        {
            _context = context;
        }

        // GET: api/inventario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventario>>> GetInventario()
        {
            var inventario = await _context.Inventario
                .Include(i => i.Producto)
                .Include(i => i.Talla)
                .ToListAsync();

            return Ok(inventario);
        }

        // GET: api/inventario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inventario>> GetInventarioItem(int id)
        {
            var inventarioItem = await _context.Inventario
                .Include(i => i.Producto)
                .Include(i => i.Talla)
                .FirstOrDefaultAsync(i => i.IdInventario == id);

            if (inventarioItem == null)
            {
                return NotFound();
            }

            return Ok(inventarioItem);
        }

        // GET: api/inventario/producto/1
        [HttpGet("producto/{idProducto}")]
        public async Task<ActionResult<IEnumerable<Inventario>>> GetInventarioByProducto(int idProducto)
        {
            var inventario = await _context.Inventario
                .Include(i => i.Talla)
                .Where(i => i.IdProducto == idProducto)
                .ToListAsync();

            if (!inventario.Any())
            {
                return NotFound();
            }

            return Ok(inventario);
        }

        // POST: api/inventario
        [HttpPost]
        public async Task<ActionResult<Inventario>> PostInventario(Inventario inventario)
        {
            _context.Inventario.Add(inventario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInventarioItem), new { id = inventario.IdInventario }, inventario);
        }

        // PUT: api/inventario/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInventario(int id, Inventario inventario)
        {
            if (id != inventario.IdInventario)
            {
                return BadRequest();
            }

            _context.Entry(inventario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventarioItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PATCH: api/inventario/5/stock
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int nuevaCantidad)
        {
            var inventarioItem = await _context.Inventario.FindAsync(id);
            if (inventarioItem == null)
            {
                return NotFound();
            }

            inventarioItem.Stock = nuevaCantidad;
            inventarioItem.FechaModificacion = DateTime.UtcNow;

            _context.Entry(inventarioItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/inventario/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventario(int id)
        {
            var inventarioItem = await _context.Inventario.FindAsync(id);
            if (inventarioItem == null)
            {
                return NotFound();
            }

            _context.Inventario.Remove(inventarioItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InventarioItemExists(int id)
        {
            return _context.Inventario.Any(e => e.IdInventario == id);
        }
    }
}