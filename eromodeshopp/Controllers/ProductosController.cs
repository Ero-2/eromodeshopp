
using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;

        public ProductosController(EromodeshopDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetProductos()
        {
            try
            {
                var productos = await _context.Productos
                    .Include(p => p.Marca)
                    .Include(p => p.Inventarios)
                        .ThenInclude(i => i.Talla)
                    .Include(p => p.ImagenesProducto) // 👈 Añade esto
                    .Select(p => new
                    {
                        idProducto = p.IdProducto,
                        nombre = p.Nombre,
                        descripcion = p.Descripcion,
                        idMarca = p.IdMarca,
                        precio = p.Precio,
                        marca = new
                        {
                            idMarca = p.Marca.IdMarca,
                            nombre = p.Marca.Nombre
                        },
                        inventarios = p.Inventarios.Select(i => new
                        {
                            stock = i.Stock,
                            talla = new
                            {
                                nombreTalla = i.Talla.NombreTalla
                            }
                        }).ToList(),
                        imagenes = p.ImagenesProducto.Select(img => new
                        {
                            idImagen = img.IdImagen,
                            urlImagen = $"{Request.Scheme}://{Request.Host}{img.UrlImagen}",
                            
                            esPrincipal = img.EsPrincipal
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(productos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetProducto(int id)
        {
            try
            {
                var producto = await _context.Productos
                    .Include(p => p.Marca)
                    .Include(p => p.Inventarios)
                        .ThenInclude(i => i.Talla)
                    .Include(p => p.ImagenesProducto) // 👈 Incluir imágenes
                    .Where(p => p.IdProducto == id)
                    .Select(p => new
                    {
                        idProducto = p.IdProducto,
                        nombre = p.Nombre,
                        descripcion = p.Descripcion,
                        idMarca = p.IdMarca,
                        precio = p.Precio,
                        marca = new
                        {
                            idMarca = p.Marca.IdMarca,
                            nombre = p.Marca.Nombre
                        },
                        inventarios = p.Inventarios.Select(i => new
                        {
                            stock = i.Stock,
                            talla = new
                            {
                                nombreTalla = i.Talla.NombreTalla
                            }
                        }).ToList(),
                        imagenes = p.ImagenesProducto
                            .OrderByDescending(img => img.EsPrincipal) // 👈 Ordenar por principal primero
                            .Select(img => new
                            {
                                idImagen = img.IdImagen,
                                // ✅ Devolver URL completa con el dominio
                                urlImagen = $"{Request.Scheme}://{Request.Host}{img.UrlImagen}",
                                esPrincipal = img.EsPrincipal
                            })
                            .ToList() // 👈 Convertir a lista
                    })
                    .FirstOrDefaultAsync();

                if (producto == null) return NotFound();
                return Ok(producto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class ProductoCreateDto
        {
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public int IdMarca { get; set; }
            public decimal Precio { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult> PostProducto([FromBody] ProductoCreateDto dto)
        {
            try
            {
                var marcaExiste = await _context.Marcas.AnyAsync(m => m.IdMarca == dto.IdMarca);
                if (!marcaExiste)
                {
                    return BadRequest(new { error = "La marca seleccionada no existe" });
                }

                var producto = new Producto
                {
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    IdMarca = dto.IdMarca,
                    Precio = dto.Precio,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
                };

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                var result = new
                {
                    idProducto = producto.IdProducto,
                    nombre = producto.Nombre,
                    descripcion = producto.Descripcion,
                    idMarca = producto.IdMarca,
                    precio = producto.Precio
                };

                return CreatedAtAction(nameof(GetProducto), new { id = producto.IdProducto }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        public class ProductoUpdateDto
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public int IdMarca { get; set; }
            public decimal Precio { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProducto(int id, [FromBody] ProductoUpdateDto dto)
        {
            if (id != dto.IdProducto) return BadRequest(new { error = "El ID no coincide" });

            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound(new { error = "Producto no encontrado" });

                var marcaExiste = await _context.Marcas.AnyAsync(m => m.IdMarca == dto.IdMarca);
                if (!marcaExiste)
                {
                    return BadRequest(new { error = "La marca seleccionada no existe" });
                }

                producto.Nombre = dto.Nombre;
                producto.Descripcion = dto.Descripcion;
                producto.IdMarca = dto.IdMarca;
                producto.Precio = dto.Precio;
                producto.FechaModificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                if (!ProductoExists(id)) return NotFound();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound();

                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.IdProducto == id);
        }

        // GET: api/productos/tallas
        [HttpGet("tallas")]
        public async Task<ActionResult> GetTallas()
        {
            try
            {
                var tallas = await _context.Tallas
                    .Select(t => new
                    {
                        idTalla = t.IdTalla,
                        nombreTalla = t.NombreTalla
                    })
                    .ToListAsync();

                return Ok(tallas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/productos/{id}/inventario
        [HttpPost("{id}/inventario")]
        public async Task<ActionResult> AddInventario(int id, [FromBody] InventarioCreateDto dto)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound(new { error = "Producto no encontrado" });

                var talla = await _context.Tallas.FindAsync(dto.IdTalla);
                if (talla == null) return BadRequest(new { error = "Talla no válida" });

                // Verificar si ya existe inventario para esta combinación
                var existing = await _context.Inventario
                    .FirstOrDefaultAsync(i => i.IdProducto == id && i.IdTalla == dto.IdTalla);

                if (existing != null)
                {
                    return BadRequest(new { error = "Ya existe inventario para esta talla en este producto" });
                }

                var inventario = new Inventario
                {
                    IdProducto = id,
                    IdTalla = dto.IdTalla,
                    Stock = dto.Stock,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
                };

                _context.Inventario.Add(inventario);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProducto), new { id }, new
                {
                    idInventario = inventario.IdInventario,
                    idProducto = inventario.IdProducto,
                    idTalla = inventario.IdTalla,
                    stock = inventario.Stock
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/productos/inventario/{id}
        [HttpPut("inventario/{id}")]
        public async Task<IActionResult> UpdateInventario(int id, [FromBody] InventarioUpdateDto dto)
        {
            try
            {
                var inventario = await _context.Inventario.FindAsync(id);
                if (inventario == null) return NotFound(new { error = "Inventario no encontrado" });

                inventario.Stock = dto.Stock;
                inventario.FechaModificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/productos/inventario/{id}
        [HttpDelete("inventario/{id}")]
        public async Task<IActionResult> DeleteInventario(int id)
        {
            try
            {
                var inventario = await _context.Inventario.FindAsync(id);
                if (inventario == null) return NotFound();

                _context.Inventario.Remove(inventario);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DTOs para inventario
        public class InventarioCreateDto
        {
            public int IdTalla { get; set; }
            public int Stock { get; set; }
        }

        public class InventarioUpdateDto
        {
            public int Stock { get; set; }
        }
    }
}