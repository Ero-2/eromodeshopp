using eromodeshopp.Data;
using eromodeshopp.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EroModeShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagenesController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ImagenesController(EromodeshopDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // POST: api/imagenes/upload/5
        [HttpPost("upload/{idProducto}")]
        public async Task<ActionResult> UploadImagen(int idProducto, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "No se recibió ningún archivo" });

                var producto = await _context.Productos.FindAsync(idProducto);
                if (producto == null)
                    return NotFound(new { error = "Producto no encontrado" });

                // Validar tipo de archivo
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                    return BadRequest(new { error = "Tipo de archivo no permitido. Usa: jpg, jpeg, png, gif, webp" });

                // Crear carpeta si no existe
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "productos");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generar nombre único
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Guardar archivo físicamente
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Guardar registro en BD
                var imagen = new ImagenProducto
                {
                    IdProducto = idProducto,
                    UrlImagen = $"/uploads/productos/{fileName}",
                    EsPrincipal = false,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
                };

                _context.ImagenesProducto.Add(imagen);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    idImagen = imagen.IdImagen,
                    urlImagen = imagen.UrlImagen,
                    esPrincipal = imagen.EsPrincipal
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // GET: api/imagenes/producto/5
        [HttpGet("producto/{idProducto}")]
        public async Task<ActionResult> GetImagenesByProducto(int idProducto)
        {
            try
            {
                var imagenes = await _context.ImagenesProducto
                    .Where(i => i.IdProducto == idProducto)
                    .OrderByDescending(i => i.EsPrincipal)
                    .Select(i => new
                    {
                        idImagen = i.IdImagen,
                        urlImagen = i.UrlImagen,
                        esPrincipal = i.EsPrincipal
                    })
                    .ToListAsync();

                return Ok(imagenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/imagenes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImagen(int id)
        {
            try
            {
                var imagen = await _context.ImagenesProducto.FindAsync(id);
                if (imagen == null) return NotFound(new { error = "Imagen no encontrada" });

                // Eliminar archivo físico
                var filePath = Path.Combine(_environment.WebRootPath, imagen.UrlImagen.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.ImagenesProducto.Remove(imagen);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Imagen eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/imagenes/principal/5
        [HttpPut("principal/{idImagen}")]
        public async Task<IActionResult> SetImagenPrincipal(int idImagen)
        {
            try
            {
                var imagen = await _context.ImagenesProducto.FindAsync(idImagen);
                if (imagen == null) return NotFound(new { error = "Imagen no encontrada" });

                // Quitar principal de otras imágenes del mismo producto
                var otrasImagenes = await _context.ImagenesProducto
                    .Where(i => i.IdProducto == imagen.IdProducto && i.IdImagen != idImagen)
                    .ToListAsync();

                foreach (var img in otrasImagenes)
                {
                    img.EsPrincipal = false;
                    img.FechaModificacion = DateTime.UtcNow;
                }

                imagen.EsPrincipal = true;
                imagen.FechaModificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Imagen principal actualizada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}