using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Api.Data;
using StoreDesk.Api.Entities;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly TradeDbContext _context;
    public OrdersController(TradeDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int? userId)
    {
        var query = _context.Orders.Include(o => o.User).Include(o => o.Status).Include(o => o.Address).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).AsQueryable();
        if (userId.HasValue) query = query.Where(o => o.UserId == userId.Value);
        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _context.Orders.Include(o => o.User).Include(o => o.Status).Include(o => o.Address).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromQuery] int userId)
    {
        if (userId <= 0) return BadRequest("Укажите пользователя (userId).");
        if (request?.Items == null || request.Items.Count == 0) return BadRequest("Добавьте хотя бы одну позицию в заказ.");
        var order = new Order { UserId = userId, StatusId = 1, AddressId = request.AddressId, CreatedAt = DateTime.UtcNow, OrderItems = request.Items.Select(item => new OrderItem { ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = item.UnitPrice }).ToList() };
        order.TotalSum = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
        _context.Orders.Add(order);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: 400);
        }
        _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = "Create", Entity = "Order", EntityId = order.Id, Details = $"Заказ №{order.Id}", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        var created = await _context.Orders.Include(o => o.User).Include(o => o.Status).Include(o => o.Address).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == order.Id);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, created ?? order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request, [FromQuery] int? userId)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.StatusId = request.StatusId;
        await _context.SaveChangesAsync();
        if (userId.HasValue) { _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = "UpdateStatus", Entity = "Order", EntityId = id, CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); }
        return NoContent();
    }
}

public class CreateOrderRequest { public int? AddressId { get; set; } public List<OrderItemDto> Items { get; set; } = new(); }
public class OrderItemDto { public int ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
public class UpdateStatusRequest { public int StatusId { get; set; } }
