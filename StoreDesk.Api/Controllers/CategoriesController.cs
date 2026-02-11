using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Api.Data;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly TradeDbContext _context;
    public CategoriesController(TradeDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetCategories() => Ok(await _context.Categories.ToListAsync());
}
