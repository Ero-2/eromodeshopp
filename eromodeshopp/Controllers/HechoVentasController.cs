// Controllers/HechoVentasController.cs
using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HechoVentasController : ControllerBase // ← Sin [Authorize]
    {
        private readonly VentasDbContext _context; // ✅ CORRECTO

        public HechoVentasController(VentasDbContext context) // ✅
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HechoVentas>>> GetHechoVentas()
        {
           return await _context.HechoVentas
                .OrderByDescending(v => v.FechaOrden)
                .ToListAsync();
        }

        [HttpGet("reporte")]
        public async Task<ActionResult> GetReporteVentas()
        {
            var reporte = await _context.HechoVentas
                .GroupBy(v => v.NombreProducto)
                .Select(g => new
                {
                    Producto = g.Key,
                    TotalUnidades = g.Sum(x => x.Cantidad),
                    TotalVentas = g.Sum(x => x.TotalProducto)
                })
                .OrderByDescending(x => x.TotalVentas)
                .ToListAsync();

            return Ok(reporte);
        }
    }
}