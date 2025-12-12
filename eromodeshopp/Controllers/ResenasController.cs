using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResenasController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;

        public ResenasController(EromodeshopDbContext context)
        {
            _context = context;
        }

        // GET: api/Resenas/producto/{idProducto}
        [HttpGet("producto/{idProducto}")]
        public async Task<ActionResult<IEnumerable<Resena>>> GetResenasPorProducto(int idProducto)
        {
            var resenas = await _context.Resenas
                .Where(r => r.IdProducto == idProducto && r.Aprobada)
                .Include(r => r.Usuario) // Opcional: para mostrar nombre del usuario
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            return Ok(resenas);
        }

        // POST: api/Resenas
        [HttpPost]
        [Authorize] // Solo usuarios autenticados pueden dejar reseña
        public async Task<ActionResult<Resena>> CreateResena([FromBody] CreateResenaModel model)
        {
            if (model == null || model.IdProducto <= 0 || model.Calificacion < 1 || model.Calificacion > 5)
            {
                return BadRequest("Datos inválidos.");
            }

            // Obtener el ID del usuario desde el token JWT
            var idUsuarioClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                return Unauthorized();
            }

            var resena = new Resena
            {
                IdProducto = model.IdProducto,
                IdUsuario = idUsuario,
                Calificacion = model.Calificacion,
                Comentario = model.Comentario,
                Fecha = DateTime.UtcNow,
                Aprobada = true, // Puedes cambiar a false si quieres moderación
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                UsuarioCreacion = "Sistema", // O usa el nombre del usuario si lo tienes
                UsuarioModificacion = "Sistema"
            };

            _context.Resenas.Add(resena);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetResenasPorProducto), new { idProducto = resena.IdProducto }, resena);
        }
    }

    // Modelo DTO para crear reseña
    public class CreateResenaModel
    {
        public int IdProducto { get; set; }
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
    }
}