namespace StoreDesk.Data.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int CategoryId { get; set; }
    public int? ManufacturerId { get; set; }
    public int? SupplierId { get; set; }
    public string? ImageUrl { get; set; }
    public Category Category { get; set; } = null!;
    public Manufacturer? Manufacturer { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
