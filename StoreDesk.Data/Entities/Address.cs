namespace StoreDesk.Data.Entities;

public class Address
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string? Apartment { get; set; }
    public string? PostalCode { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
