using Microsoft.AspNetCore.Mvc;
using StoreDesk.Api.Data;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly TradeDbContext _context;

    public HealthController(TradeDbContext context) => _context = context;

    /// <summary>
    /// Проверка подключения к БД. Откройте в браузере: http://localhost:5000/api/health
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (canConnect)
                return Ok(new { database = "connected", message = "БД подключена" });
            return StatusCode(503, new { database = "error", message = "Нет подключения к БД" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { database = "error", message = ex.Message });
        }
    }
}
