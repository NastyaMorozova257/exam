using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Api.Data;
using System.Text;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly TradeDbContext _context;
    public ReportsController(TradeDbContext context) => _context = context;

    [HttpGet("products/csv")]
    public async Task<IActionResult> ExportProductsCsv()
    {
        var products = await _context.Products.Include(p => p.Category).Include(p => p.Manufacturer).ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Id,Название,Описание,Цена,Количество,Категория,Производитель");
        foreach (var p in products) csv.AppendLine($"{p.Id},\"{p.Name}\",\"{p.Description}\",{p.Price},{p.Quantity},\"{p.Category?.Name}\",\"{p.Manufacturer?.Name}\"");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "products.csv");
    }

    [HttpGet("orders/csv")]
    public async Task<IActionResult> ExportOrdersCsv([FromQuery] int? userId)
    {
        var query = _context.Orders.Include(o => o.User).Include(o => o.Status).AsQueryable();
        if (userId.HasValue) query = query.Where(o => o.UserId == userId.Value);
        var orders = await query.ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Id,Пользователь,Статус,Дата,Сумма");
        foreach (var o in orders) csv.AppendLine($"{o.Id},\"{o.User.FullName}\",\"{o.Status.Name}\",{o.CreatedAt:yyyy-MM-dd HH:mm},{o.TotalSum}");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "orders.csv");
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int? userId, [FromQuery] int limit = 100)
    {
        var query = _context.AuditLogs.Include(a => a.User).OrderByDescending(a => a.CreatedAt).AsQueryable();
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        return Ok(await query.Take(limit).ToListAsync());
    }
}
