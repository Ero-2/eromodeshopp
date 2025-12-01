using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TallasController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;

        public TallasController(EromodeshopDbContext context)
        {
            _context = context;
        }

        // GET: api/tallas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Talla>>> GetTallas()
        {
            var tallas = await _context.Tallas.ToListAsync();
            return Ok(tallas);
        }

        // GET: api/tallas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Talla>> GetTalla(int id)
        {
            var talla = await _context.Tallas.FindAsync(id);

            if (talla == null)
            {
                return NotFound();
            }

            return Ok(talla);
        }

        // POST: api/tallas
        [HttpPost]
        public async Task<ActionResult<Talla>> PostTalla(Talla talla)
        {
            _context.Tallas.Add(talla);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTalla), new { id = talla.IdTalla }, talla);
        }

        // PUT: api/tallas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTalla(int id, Talla talla)
        {
            if (id != talla.IdTalla)
            {
                return BadRequest();
            }

            _context.Entry(talla).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TallaExists(id))
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

        // DELETE: api/tallas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTalla(int id)
        {
            var talla = await _context.Tallas.FindAsync(id);
            if (talla == null)
            {
                return NotFound();
            }

            _context.Tallas.Remove(talla);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TallaExists(int id)
        {
            return _context.Tallas.Any(e => e.IdTalla == id);
        }
    }
}