using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.EntityFrameworkCore;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagoController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;

        public PagoController(EromodeshopDbContext context)
        {
            _context = context;
        }

        [HttpPost("confirmar")]
        public async Task<IActionResult> ConfirmarPago([FromBody] ConfirmarPagoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tokenUser = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(tokenUser))
                return Unauthorized();

            if (!int.TryParse(tokenUser, out var userId))
                return Unauthorized();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orden = await _context.Orden // ← Plural
                    .FirstOrDefaultAsync(o =>
                        o.IdOrden == request.idOrden &&
                        o.IdUsuario == userId &&
                        o.status == "pendiente" &&
                        o.referencia == null
                    );

                if (orden == null)
                    return BadRequest("Orden no encontrada, no pertenece a este usuario o el pago ya fue iniciado/procesado.");

                string referencia = GenerateReference();

                orden.referencia = referencia;
                orden.status = "procesando_pago";
                _context.Orden.Update(orden); // ← Plural

                var detallePago = new DetallePago
                {
                    idOrden = orden.IdOrden,
                    formaPago = request.formaPago,
                    status = "pendiente",
                    referencia = referencia
                };

                _context.DetallePago.Add(detallePago); // ← Plural
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Pago iniciado exitosamente. Esperando confirmación.",
                    idOrden = orden.IdOrden,
                    referencia = referencia,
                    status = orden.status
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Error interno al procesar el pago", details = ex.Message });
            }
        }

        public class ConfirmarPagoRequest
        {
            public int idOrden { get; set; }
            public string formaPago { get; set; } = string.Empty;
            public string? numeroTarjeta { get; set; }
            public string? fechaExpiracion { get; set; }
            public string? cvv { get; set; }
            public string? referenciaOxxo { get; set; }
            public string? cuentaTransferencia { get; set; }
        }

        private string GenerateReference()
        {
            return $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
        }
    }
}