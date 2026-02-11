using System.Windows;
using StoreDesk.Data.Entities;

namespace StoreDesk.Desktop.Views;

public partial class OrderStatusWindow : Window
{
    public int SelectedStatusId => StatusComboBox.SelectedValue is int id ? id : 0;

    public OrderStatusWindow(List<Status> statuses, int currentStatusId)
    {
        InitializeComponent();
        StatusComboBox.ItemsSource = statuses;
        foreach (var s in statuses)
        {
            if (s.Id == currentStatusId) { StatusComboBox.SelectedItem = s; break; }
        }
        if (StatusComboBox.SelectedItem == null && statuses.Count > 0)
            StatusComboBox.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
