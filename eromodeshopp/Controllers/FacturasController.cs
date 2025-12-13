using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos los endpoints requieren autenticación
    public class FacturasController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;
        private readonly ILogger<FacturasController> _logger;

        public FacturasController(EromodeshopDbContext context, ILogger<FacturasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/facturas
        // Obtener todas las facturas (para administradores)
        [HttpGet]
        [Authorize(Roles = "Admin")] // Solo administradores pueden ver todas las facturas
        public async Task<ActionResult<IEnumerable<FacturaDto>>> GetFacturas()
        {
            try
            {
                var facturas = await _context.Facturas
                    .Include(f => f.Orden)
                        .ThenInclude(o => o.Usuario)
                    .OrderByDescending(f => f.FechaEmision)
                    .Select(f => new FacturaDto
                    {
                        IdFactura = f.IdFactura,
                        IdOrden = f.IdOrden,
                        NumeroFactura = f.NumeroFactura,
                        FechaEmision = f.FechaEmision,
                        RUC_DNI_Cliente = f.RUC_DNI_Cliente,
                        NombreCliente = f.NombreCliente,
                        DireccionCliente = f.DireccionCliente,
                        TotalBruto = f.TotalBruto,
                        Impuestos = f.Impuestos,
                        TotalNeto = f.TotalNeto,
                        Estado = f.Estado,
                        PDF_URL = f.PDF_URL,
                        FechaCreacion = f.FechaCreacion,
                        NumeroOrden = f.Orden != null ? f.Orden.IdOrden.ToString() : "N/A",
                        EmailCliente = f.Orden != null && f.Orden.Usuario != null ? f.Orden.Usuario.Email : "N/A"
                    })
                    .ToListAsync();

                return Ok(facturas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener facturas");
                return StatusCode(500, new { error = "Error interno del servidor al obtener facturas" });
            }
        }

        // GET: api/facturas/mis-facturas
        // Obtener facturas del usuario autenticado
        [HttpGet("mis-facturas")]
        public async Task<ActionResult<IEnumerable<FacturaDto>>> GetMisFacturas()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { error = "Token inválido o usuario no encontrado" });
                }

                var facturas = await _context.Facturas
                    .Include(f => f.Orden)
                        .ThenInclude(o => o.Usuario)
                    .Where(f => f.Orden != null && f.Orden.IdUsuario == userId)
                    .OrderByDescending(f => f.FechaEmision)
                    .Select(f => new FacturaDto
                    {
                        IdFactura = f.IdFactura,
                        IdOrden = f.IdOrden,
                        NumeroFactura = f.NumeroFactura,
                        FechaEmision = f.FechaEmision,
                        RUC_DNI_Cliente = f.RUC_DNI_Cliente,
                        NombreCliente = f.NombreCliente,
                        DireccionCliente = f.DireccionCliente,
                        TotalBruto = f.TotalBruto,
                        Impuestos = f.Impuestos,
                        TotalNeto = f.TotalNeto,
                        Estado = f.Estado,
                        PDF_URL = f.PDF_URL,
                        FechaCreacion = f.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(facturas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener mis facturas para el usuario");
                return StatusCode(500, new { error = "Error interno del servidor al obtener tus facturas" });
            }
        }

        // GET: api/facturas/{id}
        // Obtener una factura específica por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<FacturaDetalleDto>> GetFactura(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { error = "Token inválido o usuario no encontrado" });
                }

                var factura = await _context.Facturas
                    .Include(f => f.Orden)
                        .ThenInclude(o => o.Usuario)
                    .Include(f => f.Orden)
                        .ThenInclude(o => o.DetallesOrden)
                        .ThenInclude(d => d.Inventario)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(f => f.IdFactura == id);

                if (factura == null)
                {
                    return NotFound(new { error = "Factura no encontrada" });
                }

                // Verificar permisos: admin puede ver todas, usuario solo sus propias facturas
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && (factura.Orden == null || factura.Orden.IdUsuario != userId))
                {
                    return Forbid();
                }

                var detallesOrden = factura.Orden?.DetallesOrden?.Select(d => new DetalleFacturaDto
                {
                    Producto = d.Inventario?.Producto?.Nombre ?? "Producto no disponible",
                    Talla = d.Inventario?.Talla?.NombreTalla ?? "N/A",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario
                }).ToList() ?? new List<DetalleFacturaDto>();

                var facturaDetalle = new FacturaDetalleDto
                {
                    IdFactura = factura.IdFactura,
                    IdOrden = factura.IdOrden,
                    NumeroFactura = factura.NumeroFactura,
                    FechaEmision = factura.FechaEmision,
                    RUC_DNI_Cliente = factura.RUC_DNI_Cliente,
                    NombreCliente = factura.NombreCliente,
                    DireccionCliente = factura.DireccionCliente,
                    TotalBruto = factura.TotalBruto,
                    Impuestos = factura.Impuestos,
                    TotalNeto = factura.TotalNeto,
                    Estado = factura.Estado,
                    PDF_URL = factura.PDF_URL,
                    FechaCreacion = factura.FechaCreacion,
                    FechaModificacion = factura.FechaModificacion,
                    Detalles = detallesOrden,
                    MetodoPago = factura.Orden?.MetodoPago ?? "N/A",
                    FechaOrden = factura.Orden?.FechaOrden ?? DateTime.MinValue,
                    EmailCliente = factura.Orden?.Usuario?.Email ?? "N/A"
                };

                return Ok(facturaDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener factura con ID {id}");
                return StatusCode(500, new { error = "Error interno del servidor al obtener la factura" });
            }
        }

        // POST: api/facturas/generar/{idOrden}
        // Generar una nueva factura para una orden
        [HttpPost("generar/{idOrden}")]
        public async Task<ActionResult<FacturaDto>> GenerarFactura(int idOrden, [FromBody] GenerarFacturaRequest request)
        {
            try
            {
                _logger.LogInformation($"Iniciando generación de factura para orden {idOrden}");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { error = "Token inválido o usuario no encontrado" });
                }

                _logger.LogInformation($"Usuario ID: {userId}, RUC/DNI: {request.RUC_DNI_Cliente}, Nombre: {request.NombreCliente}");

                // Validación manual adicional
                // ... (mantén tus validaciones actuales)

                // Buscar la orden
                _logger.LogInformation($"Buscando orden {idOrden}...");
                var orden = await _context.Orden
                    .Include(o => o.Usuario)
                    .Include(o => o.DetallesOrden)
                        .ThenInclude(d => d.Inventario)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

                if (orden == null)
                {
                    _logger.LogWarning($"Orden {idOrden} no encontrada");
                    return NotFound(new { error = "Orden no encontrada" });
                }

                _logger.LogInformation($"Orden encontrada: ID {orden.IdOrden}, Total: {orden.Total}, Usuario: {orden.IdUsuario}");

                // Verificar que la orden pertenezca al usuario o sea admin
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && orden.IdUsuario != userId)
                {
                    _logger.LogWarning($"Usuario {userId} no tiene permiso para la orden {idOrden} (propietario: {orden.IdUsuario})");
                    return Forbid();
                }

                // Verificar que no exista ya una factura para esta orden
                _logger.LogInformation($"Verificando si ya existe factura para orden {idOrden}...");
                var facturaExistente = await _context.Facturas
                    .FirstOrDefaultAsync(f => f.IdOrden == idOrden);

                if (facturaExistente != null)
                {
                    _logger.LogInformation($"Ya existe factura {facturaExistente.IdFactura} para orden {idOrden}");
                    return Conflict(new
                    {
                        error = "Ya existe una factura para esta orden",
                        idFactura = facturaExistente.IdFactura,
                        numeroFactura = facturaExistente.NumeroFactura
                    });
                }

                // Calcular totales
                decimal totalBruto = orden.Total;
                decimal impuestos = totalBruto * 0.16m; // 16% de IVA
                decimal totalNeto = totalBruto + impuestos;

                _logger.LogInformation($"Cálculos: Bruto={totalBruto}, Impuestos={impuestos}, Neto={totalNeto}");

                // Generar número de factura único
                string numeroFactura = GenerarNumeroFactura();
                _logger.LogInformation($"Número de factura generado: {numeroFactura}");

                // Crear la factura
                var factura = new Factura
                {
                    IdOrden = idOrden,
                    NumeroFactura = numeroFactura,
                    FechaEmision = DateTime.Today,
                    RUC_DNI_Cliente = request.RUC_DNI_Cliente.Trim(),
                    NombreCliente = request.NombreCliente.Trim(),
                    DireccionCliente = request.DireccionCliente.Trim(),
                    TotalBruto = totalBruto,
                    Impuestos = impuestos,
                    TotalNeto = totalNeto,
                    Estado = "emitida",
                    PDF_URL = null,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow,
                    UsuarioCreacion = User.Identity?.Name ?? "sistema",
                    UsuarioModificacion = User.Identity?.Name ?? "sistema"
                };

                _logger.LogInformation($"Creando factura: {System.Text.Json.JsonSerializer.Serialize(factura)}");

                try
                {
                    _context.Facturas.Add(factura);
                    _logger.LogInformation("Guardando factura en base de datos...");
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Factura guardada exitosamente con ID: {factura.IdFactura}");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, $"Error de base de datos al guardar factura: {dbEx.InnerException?.Message}");

                    // Detalles específicos de error de base de datos
                    if (dbEx.InnerException != null)
                    {
                        return StatusCode(500, new
                        {
                            error = "Error de base de datos al guardar la factura",
                            details = dbEx.InnerException.Message,
                            tipo = "DbUpdateException"
                        });
                    }

                    throw;
                }

                // Actualizar estado de la orden
                _logger.LogInformation($"Actualizando estado de orden {idOrden} a 'facturada'...");
                orden.status = "facturada";
                orden.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Estado de orden actualizado exitosamente");

                var facturaDto = new FacturaDto
                {
                    IdFactura = factura.IdFactura,
                    IdOrden = factura.IdOrden,
                    NumeroFactura = factura.NumeroFactura,
                    FechaEmision = factura.FechaEmision,
                    RUC_DNI_Cliente = factura.RUC_DNI_Cliente,
                    NombreCliente = factura.NombreCliente,
                    DireccionCliente = factura.DireccionCliente,
                    TotalBruto = factura.TotalBruto,
                    Impuestos = factura.Impuestos,
                    TotalNeto = factura.TotalNeto,
                    Estado = factura.Estado,
                    PDF_URL = factura.PDF_URL,
                    FechaCreacion = factura.FechaCreacion
                };

                _logger.LogInformation($"Factura generada exitosamente: {System.Text.Json.JsonSerializer.Serialize(facturaDto)}");

                return CreatedAtAction(nameof(GetFactura), new { id = factura.IdFactura }, facturaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error crítico al generar factura para orden {idOrden}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"InnerException: {ex.InnerException.Message}");
                    _logger.LogError($"InnerException StackTrace: {ex.InnerException.StackTrace}");
                }

                return StatusCode(500, new
                {
                    error = "Error interno del servidor al generar la factura",
                    details = ex.Message,
                    innerDetails = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // PUT: api/facturas/{id}/estado
        // Actualizar estado de una factura (solo admin)
        [HttpPut("{id}/estado")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActualizarEstadoFactura(int id, [FromBody] ActualizarEstadoRequest request)
        {
            try
            {
                var factura = await _context.Facturas.FindAsync(id);
                if (factura == null)
                {
                    return NotFound(new { error = "Factura no encontrada" });
                }

                // Validar estado
                var estadosValidos = new[] { "emitida", "pagada", "anulada", "vencida" };
                if (!estadosValidos.Contains(request.Estado?.ToLower()))
                {
                    return BadRequest(new { error = $"Estado inválido. Estados válidos: {string.Join(", ", estadosValidos)}" });
                }

                factura.Estado = request.Estado.ToLower();
                factura.FechaModificacion = DateTime.UtcNow;
                factura.UsuarioModificacion = User.Identity?.Name ?? "sistema";

                await _context.SaveChangesAsync();

                return Ok(new { message = "Estado de factura actualizado correctamente", nuevoEstado = factura.Estado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar estado de factura {id}");
                return StatusCode(500, new { error = "Error interno del servidor al actualizar el estado" });
            }
        }

        // PUT: api/facturas/{id}/pdf
        // Actualizar URL del PDF de la factura (solo admin)
        [HttpPut("{id}/pdf")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActualizarPDFFactura(int id, [FromBody] ActualizarPDFRequest request)
        {
            try
            {
                var factura = await _context.Facturas.FindAsync(id);
                if (factura == null)
                {
                    return NotFound(new { error = "Factura no encontrada" });
                }

                if (string.IsNullOrWhiteSpace(request.PDF_URL))
                {
                    return BadRequest(new { error = "La URL del PDF es requerida" });
                }

                factura.PDF_URL = request.PDF_URL.Trim();
                factura.FechaModificacion = DateTime.UtcNow;
                factura.UsuarioModificacion = User.Identity?.Name ?? "sistema";

                await _context.SaveChangesAsync();

                return Ok(new { message = "URL del PDF actualizada correctamente", pdfUrl = factura.PDF_URL });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar PDF de factura {id}");
                return StatusCode(500, new { error = "Error interno del servidor al actualizar el PDF" });
            }
        }

        // DELETE: api/facturas/{id} (solo admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFactura(int id)
        {
            try
            {
                var factura = await _context.Facturas.FindAsync(id);
                if (factura == null)
                {
                    return NotFound(new { error = "Factura no encontrada" });
                }

                _context.Facturas.Remove(factura);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Factura eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar factura {id}");
                return StatusCode(500, new { error = "Error interno del servidor al eliminar la factura" });
            }
        }

        // GET: api/facturas/estadisticas
        // Obtener estadísticas de facturas (solo admin)
        [HttpGet("estadisticas")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EstadisticasFacturasDto>> GetEstadisticas()
        {
            try
            {
                var hoy = DateTime.Today;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                var finMes = inicioMes.AddMonths(1).AddDays(-1);

                var totalFacturas = await _context.Facturas.CountAsync();
                var facturasEsteMes = await _context.Facturas
                    .Where(f => f.FechaEmision >= inicioMes && f.FechaEmision <= finMes)
                    .CountAsync();

                var ingresosTotales = await _context.Facturas
                    .Where(f => f.Estado == "pagada")
                    .SumAsync(f => f.TotalNeto);

                var ingresosEsteMes = await _context.Facturas
                    .Where(f => f.Estado == "pagada" && f.FechaEmision >= inicioMes && f.FechaEmision <= finMes)
                    .SumAsync(f => f.TotalNeto);

                var facturasPorEstado = await _context.Facturas
                    .GroupBy(f => f.Estado)
                    .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                    .ToListAsync();

                var estadisticas = new EstadisticasFacturasDto
                {
                    TotalFacturas = totalFacturas,
                    FacturasEsteMes = facturasEsteMes,
                    IngresosTotales = ingresosTotales,
                    IngresosEsteMes = ingresosEsteMes,
                    FacturasPorEstado = facturasPorEstado.ToDictionary(x => x.Estado, x => x.Cantidad)
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de facturas");
                return StatusCode(500, new { error = "Error interno del servidor al obtener estadísticas" });
            }
        }

        // Método auxiliar para generar número de factura único
        private string GenerarNumeroFactura()
        {
            var año = DateTime.Now.Year;
            var mes = DateTime.Now.Month.ToString("D2");
            var secuencia = _context.Facturas
                .Where(f => f.FechaEmision.Year == año && f.FechaEmision.Month == DateTime.Now.Month)
                .Count() + 1;

            return $"FAC-{año}{mes}-{secuencia.ToString("D4")}";
        }
    }

    // DTOs
    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public int IdOrden { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string RUC_DNI_Cliente { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string DireccionCliente { get; set; } = string.Empty;
        public decimal TotalBruto { get; set; }
        public decimal Impuestos { get; set; }
        public decimal TotalNeto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? PDF_URL { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? NumeroOrden { get; set; }
        public string? EmailCliente { get; set; }
    }

    public class FacturaDetalleDto : FacturaDto
    {
        public DateTime FechaModificacion { get; set; }
        public List<DetalleFacturaDto> Detalles { get; set; } = new();
        public string MetodoPago { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public string EmailCliente { get; set; } = string.Empty;
    }

    public class DetalleFacturaDto
    {
        public string Producto { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class GenerarFacturaRequest
    {
        [Required(ErrorMessage = "El RUC/DNI del cliente es requerido")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El RUC/DNI debe tener entre 5 y 20 caracteres")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El RUC/DNI debe contener solo números")]
        public string RUC_DNI_Cliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del cliente es requerido")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")] // Cambiar de 200 a 255
        public string NombreCliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección del cliente es requerida")]
        [MinLength(5, ErrorMessage = "La dirección debe tener al menos 5 caracteres")] // Quitar StringLength porque es text
        public string DireccionCliente { get; set; } = string.Empty;
    }

    public class ActualizarEstadoRequest
    {
        [Required(ErrorMessage = "El estado es requerido")]
        public string Estado { get; set; } = string.Empty;
    }

    public class ActualizarPDFRequest
    {
        [Required(ErrorMessage = "La URL del PDF es requerida")]
        [Url(ErrorMessage = "Debe ser una URL válida")]
        public string PDF_URL { get; set; } = string.Empty;
    }

    public class EstadisticasFacturasDto
    {
        public int TotalFacturas { get; set; }
        public int FacturasEsteMes { get; set; }
        public decimal IngresosTotales { get; set; }
        public decimal IngresosEsteMes { get; set; }
        public Dictionary<string, int> FacturasPorEstado { get; set; } = new();
    }
}