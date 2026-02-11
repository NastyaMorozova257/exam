using System.Linq;
using System.Windows;
using StoreDesk.Data.Entities;

namespace StoreDesk.Desktop.Views;

public partial class ProductEditWindow : Window
{
    public Product Product { get; set; }
    public ProductEditWindow(Product product, System.Collections.Generic.IEnumerable<Category> categories)
    {
        InitializeComponent();
        Product = product;
        var realCategories = categories?.Where(c => c.Id != 0).ToList() ?? new System.Collections.Generic.List<Category>();
        if (realCategories.Count > 0 && product.CategoryId <= 0)
            product.CategoryId = realCategories[0].Id;
        CategoryComboBox.ItemsSource = realCategories;
        DataContext = Product;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Product.Name)) { MessageBox.Show("Введите название товара.", "Валидация"); return; }
        if (Product.Price < 0) { MessageBox.Show("Цена не может быть отрицательной.", "Валидация"); return; }
        if (Product.Quantity < 0) { MessageBox.Show("Количество не может быть отрицательным.", "Валидация"); return; }
        DialogResult = true;
        Close();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
