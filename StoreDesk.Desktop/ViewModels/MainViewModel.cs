using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Data.Entities;
using StoreDesk.Desktop.Data;
using StoreDesk.Desktop.Models;
using StoreDesk.Desktop.Services;
using StoreDesk.Desktop.Views;
using Microsoft.Win32;

namespace StoreDesk.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly StoreDesk.Data.Entities.User? _currentUser;

    [ObservableProperty] private ObservableCollection<StoreDesk.Data.Entities.Product> products = new();
    [ObservableProperty] private ObservableCollection<StoreDesk.Data.Entities.Category> categories = new();
    [ObservableProperty] private ObservableCollection<StoreDesk.Data.Entities.Order> orders = new();
    [ObservableProperty] private ObservableCollection<StoreDesk.Data.Entities.Address> addresses = new();
    [ObservableProperty] private ObservableCollection<StoreDesk.Data.Entities.Status> statuses = new();
    [ObservableProperty] private StoreDesk.Data.Entities.Product? selectedProduct;
    [ObservableProperty] private StoreDesk.Data.Entities.Order? selectedOrder;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private StoreDesk.Data.Entities.Category? selectedCategory;
    [ObservableProperty] private string sortBy = "name";
    [ObservableProperty] private bool ascending = true;
    [ObservableProperty] private bool canEditProducts;
    [ObservableProperty] private bool canManageOrders;
    [ObservableProperty] private bool canSearchAndFilter = true;
    [ObservableProperty] private bool canViewOrdersList = true;
    [ObservableProperty] private int selectedTabIndex;

    public MainViewModel()
    {
        _currentUser = App.Current.Properties["CurrentUser"] as StoreDesk.Data.Entities.User;
        if (_currentUser != null)
        {
            var isAdmin = _currentUser.RoleId == 1;
            var isManager = _currentUser.RoleId == 2;
            CanEditProducts = isAdmin;
            CanManageOrders = isAdmin;
            CanSearchAndFilter = isAdmin || isManager;
            CanViewOrdersList = isAdmin || isManager;
        }
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await LoadProductsAsync();
        await LoadCategoriesAsync();
        if (CanViewOrdersList)
        {
            await LoadOrdersAsync();
            await LoadAddressesAsync();
            await LoadStatusesAsync();
        }
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        try
        {
            await using var db = DbConfig.CreateContext();
            var query = db.Products.Include(p => p.Category).Include(p => p.Manufacturer).Include(p => p.Supplier).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(p => p.Name.Contains(SearchText) || (p.Description != null && p.Description.Contains(SearchText)));
            if (SelectedCategory != null && SelectedCategory.Id != 0)
                query = query.Where(p => p.CategoryId == SelectedCategory.Id);
            var byPrice = SortBy == "price";
            query = byPrice
                ? (Ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price))
                : (Ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name));
            var list = await query.ToListAsync();
            Products.Clear();
            foreach (var p in list) Products.Add(p);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка загрузки товаров: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        try
        {
            await using var db = DbConfig.CreateContext();
            var list = await db.Categories.OrderBy(c => c.Name).ToListAsync();
            Categories.Clear();
            Categories.Add(new StoreDesk.Data.Entities.Category { Id = 0, Name = "Все категории" });
            foreach (var c in list) Categories.Add(c);
            if (SelectedCategory == null) SelectedCategory = Categories.First();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка загрузки категорий: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        try
        {
            await using var db = DbConfig.CreateContext();
            var query = db.Orders.Include(o => o.User).Include(o => o.Status).Include(o => o.Address).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).AsQueryable();
            var uid = _currentUser?.Id ?? 0;
            var isAdminOrManager = _currentUser != null && (_currentUser.RoleId == 1 || _currentUser.RoleId == 2);
            if (!isAdminOrManager)
                query = query.Where(o => o.UserId == uid);
            var list = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            Orders.Clear();
            foreach (var o in list) Orders.Add(o);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка загрузки заказов: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task LoadAddressesAsync()
    {
        try
        {
            await using var db = DbConfig.CreateContext();
            var list = await db.Addresses.OrderBy(a => a.City).ToListAsync();
            Addresses.Clear();
            foreach (var a in list) Addresses.Add(a);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task LoadStatusesAsync()
    {
        try
        {
            await using var db = DbConfig.CreateContext();
            var list = await db.Statuses.OrderBy(s => s.Id).ToListAsync();
            Statuses.Clear();
            foreach (var s in list) Statuses.Add(s);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        await using var db = DbConfig.CreateContext();
        var allProducts = await db.Products.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync();
        var addressList = Addresses.ToList();
        var d = new OrderCreateWindow(allProducts, addressList);
        if (d.ShowDialog() != true || d.OrderItems == null || d.OrderItems.Count == 0) return;
        var userId = _currentUser?.Id ?? 0;
        if (userId <= 0) { MessageBox.Show("Для создания заказа необходимо войти в систему.", "Внимание"); return; }
        try
        {
            var order = new StoreDesk.Data.Entities.Order
            {
                UserId = userId,
                StatusId = 1,
                AddressId = d.SelectedAddressId,
                CreatedAt = DateTime.UtcNow,
                OrderItems = d.OrderItems.Select(i => new StoreDesk.Data.Entities.OrderItem { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList()
            };
            order.TotalSum = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            _ = LoadOrdersAsync();
            MessageBox.Show("Заказ создан", "Успех");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при создании заказа: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task ChangeOrderStatusAsync()
    {
        if (SelectedOrder == null) { MessageBox.Show("Выберите заказ.", "Внимание"); return; }
        var d = new OrderStatusWindow(Statuses.ToList(), SelectedOrder.StatusId);
        if (d.ShowDialog() != true) return;
        try
        {
            await using var db = DbConfig.CreateContext();
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == SelectedOrder.Id);
            if (order != null)
            {
                order.StatusId = d.SelectedStatusId;
                await db.SaveChangesAsync();
                _ = LoadOrdersAsync();
                MessageBox.Show("Статус обновлён.", "Успех");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        var realCategories = Categories.Where(c => c.Id != 0).ToList();
        var product = new StoreDesk.Data.Entities.Product { CategoryId = realCategories.Count > 0 ? realCategories[0].Id : 1 };
        var d = new ProductEditWindow(product, realCategories);
        if (d.ShowDialog() != true) return;
        try
        {
            await using var db = DbConfig.CreateContext();
            db.Products.Add(d.Product);
            await db.SaveChangesAsync();
            _ = LoadProductsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при добавлении: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task EditProductAsync()
    {
        if (SelectedProduct == null) return;
        var realCategories = Categories.Where(c => c.Id != 0).ToList();
        var d = new ProductEditWindow(SelectedProduct, realCategories);
        if (d.ShowDialog() != true) return;
        try
        {
            await using var db = DbConfig.CreateContext();
            var ent = await db.Products.FirstOrDefaultAsync(p => p.Id == d.Product.Id);
            if (ent != null)
            {
                ent.Name = d.Product.Name;
                ent.Description = d.Product.Description;
                ent.Price = d.Product.Price;
                ent.Quantity = d.Product.Quantity;
                ent.CategoryId = d.Product.CategoryId;
                ent.ManufacturerId = d.Product.ManufacturerId;
                ent.SupplierId = d.Product.SupplierId;
                ent.ImageUrl = d.Product.ImageUrl;
                await db.SaveChangesAsync();
                _ = LoadProductsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка сохранения: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;
        if (MessageBox.Show("Удалить товар?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try
        {
            await using var db = DbConfig.CreateContext();
            var ent = await db.Products.FirstOrDefaultAsync(p => p.Id == SelectedProduct.Id);
            if (ent != null)
            {
                db.Products.Remove(ent);
                await db.SaveChangesAsync();
                _ = LoadProductsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка удаления: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка");
        }
    }

    [RelayCommand]
    private async Task ExportProductsCsvAsync()
    {
        try
        {
            // Папка без имени пользователя и OneDrive в пути (общий рабочий стол)
            var saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            if (string.IsNullOrEmpty(saveFolder))
                saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var defaultFileName = Path.Combine(saveFolder, "products.csv");
            var d = new SaveFileDialog 
            { 
                Filter = "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx", 
                FileName = defaultFileName,
                InitialDirectory = saveFolder
            };
            if (d.ShowDialog() != true) return;
            var list = Products.ToList();
            if (d.FilterIndex == 2)
                new ExportService().ExportProductsToExcel(list, d.FileName);
            else
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Id,Название,Описание,Цена,Количество,Категория,Производитель");
                foreach (var p in list)
                    csv.AppendLine($"{p.Id},\"{p.Name}\",\"{p.Description}\",{p.Price},{p.Quantity},\"{p.Category?.Name}\",\"{p.Manufacturer?.Name}\"");
                await File.WriteAllTextAsync(d.FileName, csv.ToString(), System.Text.Encoding.UTF8);
            }
            MessageBox.Show("Экспорт завершён", "Успех");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand] private void SelectProductsTab() => SelectedTabIndex = 0;
    [RelayCommand] private void SelectOrdersTab() => SelectedTabIndex = 1;

    [RelayCommand]
    private void Logout()
    {
        App.Current.Properties.Remove("CurrentUser");
        new LoginWindow().Show();
        Application.Current.Windows.OfType<MainWindow>().First().Close();
    }

    public bool SortByName { get => SortBy == "name"; set { if (value) SortBy = "name"; } }
    public bool SortByPrice { get => SortBy == "price"; set { if (value) SortBy = "price"; } }

    partial void OnSearchTextChanged(string value) => _ = LoadProductsAsync();
    partial void OnSelectedCategoryChanged(StoreDesk.Data.Entities.Category? value) => _ = LoadProductsAsync();
    partial void OnAscendingChanged(bool value) => _ = LoadProductsAsync();
    partial void OnSortByChanged(string value) { OnPropertyChanged(nameof(SortByName)); OnPropertyChanged(nameof(SortByPrice)); }
}
