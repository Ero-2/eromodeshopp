using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class SincronizarController : ControllerBase
    {
        private readonly string _ventasConnectionString;

        public SincronizarController(IConfiguration configuration)
        {
            _ventasConnectionString = configuration.GetConnectionString("VentasConnection");
        }

        [HttpPost("ventas")]
        public async Task<IActionResult> SincronizarVentas()
        {
            try
            {
                using var conn = new SqlConnection(_ventasConnectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC SincronizarVentasDesdePostgreSQL", conn);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { mensaje = "✅ Ventas sincronizadas correctamente." });
            }
            catch (Exception ex)
            {
                // En desarrollo, puedes devolver el error. En producción, logea e ignora detalles.
                return StatusCode(500, new { error = "❌ Error al sincronizar ventas.", detalle = ex.Message });
            }
        }
    }
}