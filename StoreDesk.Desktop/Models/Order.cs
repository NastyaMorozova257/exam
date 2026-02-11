namespace StoreDesk.Desktop.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int StatusId { get; set; }
    public int? AddressId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalSum { get; set; }
    public User? User { get; set; }
    public Status? Status { get; set; }
    public Address? Address { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
}

public class OrderItem { public int Id { get; set; } public int OrderId { get; set; } public int ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } public Product? Product { get; set; } }
public class Status { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class Address { public int Id { get; set; } public string City { get; set; } = string.Empty; public string Street { get; set; } = string.Empty; public string Building { get; set; } = string.Empty; public string? Apartment { get; set; } public string? PostalCode { get; set; } }

/// <summary>DTO для создания заказа через API.</summary>
public class CreateOrderRequestDto
{
    public int? AddressId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
