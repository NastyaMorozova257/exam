namespace StoreDesk.Api.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public int? AddressId { get; set; }
    public Address? Address { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
