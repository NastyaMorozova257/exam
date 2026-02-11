using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Api.Data;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusesController : ControllerBase
{
    private readonly TradeDbContext _context;
    public StatusesController(TradeDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetStatuses() => Ok(await _context.Statuses.ToListAsync());
}
