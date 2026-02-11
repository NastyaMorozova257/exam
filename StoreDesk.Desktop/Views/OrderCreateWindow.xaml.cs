using System.Collections.ObjectModel;
using System.Windows;
using StoreDesk.Data.Entities;
using StoreDesk.Desktop.Models;

namespace StoreDesk.Desktop.Views;

public partial class OrderCreateWindow : Window
{
    private readonly List<StoreDesk.Data.Entities.Product> _products;
    private readonly ObservableCollection<OrderLineViewModel> _lines = new();

    public int? SelectedAddressId
    {
        get => AddressComboBox.SelectedValue as int?;
        set => AddressComboBox.SelectedValue = value;
    }

    public List<OrderItemDto>? OrderItems
    {
        get
        {
            var list = new List<OrderItemDto>();
            foreach (var line in _lines)
            {
                if (line.ProductId > 0 && line.Quantity > 0)
                    list.Add(new OrderItemDto { ProductId = line.ProductId, Quantity = line.Quantity, UnitPrice = line.UnitPrice });
            }
            return list.Count > 0 ? list : null;
        }
    }

    public OrderCreateWindow(List<StoreDesk.Data.Entities.Product> products, List<StoreDesk.Data.Entities.Address> addresses)
    {
        _products = products;
        InitializeComponent();
        AddressComboBox.ItemsSource = addresses;
        if (addresses.Count > 0) AddressComboBox.SelectedIndex = 0;
        ItemsGrid.ItemsSource = _lines;
        ProductColumn.ItemsSource = _products;
        AddRow_Click(null!, null!);
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        _lines.Add(new OrderLineViewModel { ProductId = _products.Count > 0 ? _products[0].Id : 0, Quantity = 1, UnitPrice = _products.Count > 0 ? _products[0].Price : 0, Products = _products });
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is OrderLineViewModel line)
            _lines.Remove(line);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (OrderItems == null || OrderItems.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы одну позицию в заказ.", "Внимание");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class OrderLineViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private int _productId;
    private int _quantity;
    private decimal _unitPrice;
    public List<StoreDesk.Data.Entities.Product> Products { get; set; } = new();

    public int ProductId
    {
        get => _productId;
        set { _productId = value; var p = Products.FirstOrDefault(x => x.Id == value); if (p != null) UnitPrice = p.Price; OnPropertyChanged(nameof(ProductId)); }
    }
    public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); } }
    public decimal UnitPrice { get => _unitPrice; set { _unitPrice = value; OnPropertyChanged(nameof(UnitPrice)); } }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
