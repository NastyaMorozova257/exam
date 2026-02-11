using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Api.Data;
using StoreDesk.Api.Entities;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly TradeDbContext _context;
    public ProductsController(TradeDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] string? sortBy = "name", [FromQuery] bool ascending = true)
    {
        var query = _context.Products.Include(p => p.Category).Include(p => p.Manufacturer).Include(p => p.Supplier).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search));
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        query = sortBy?.ToLower() == "price" ? (ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price)) : (ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name));
        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _context.Products.Include(p => p.Category).Include(p => p.Manufacturer).Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto? dto, [FromQuery] int? userId)
    {
        if (dto == null) return BadRequest(new { detail = "Тело запроса пусто или неверный формат JSON." });
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { detail = "Укажите название товара." });
        if (dto.CategoryId <= 0) return BadRequest(new { detail = "Выберите категорию товара." });
        var entity = new Product
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Price = dto.Price >= 0 ? dto.Price : 0,
            Quantity = dto.Quantity >= 0 ? dto.Quantity : 0,
            CategoryId = dto.CategoryId,
            ManufacturerId = dto.ManufacturerId > 0 ? dto.ManufacturerId : null,
            SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null,
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim()
        };
        _context.Products.Add(entity);
        await _context.SaveChangesAsync();
        if (userId.HasValue) { _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = "Create", Entity = "Product", EntityId = entity.Id, Details = entity.Name, CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); }
        var created = await _context.Products.Include(p => p.Category).Include(p => p.Manufacturer).Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == entity.Id);
        return CreatedAtAction(nameof(GetProduct), new { id = entity.Id }, created ?? entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product, [FromQuery] int? userId)
    {
        if (id != product.Id) return BadRequest();
        _context.Entry(product).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            if (userId.HasValue) { _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = "Update", Entity = "Product", EntityId = product.Id, Details = product.Name, CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); }
        }
        catch (DbUpdateConcurrencyException) { if (!await _context.Products.AnyAsync(e => e.Id == id)) return NotFound(); throw; }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id, [FromQuery] int? userId)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        if (userId.HasValue) { _context.AuditLogs.Add(new AuditLog { UserId = userId, Action = "Delete", Entity = "Product", EntityId = id, Details = product.Name, CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); }
        return NoContent();
    }
}

/// <summary>DTO для создания товара — только нужные поля, без навигационных свойств.</summary>
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int CategoryId { get; set; }
    public int? ManufacturerId { get; set; }
    public int? SupplierId { get; set; }
    public string? ImageUrl { get; set; }
}
