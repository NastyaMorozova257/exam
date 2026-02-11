namespace StoreDesk.Api.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int StatusId { get; set; }
    public int? AddressId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalSum { get; set; }
    public User User { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
