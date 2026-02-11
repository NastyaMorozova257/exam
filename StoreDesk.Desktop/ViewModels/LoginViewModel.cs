using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Data.Entities;
using StoreDesk.Desktop.Data;
using StoreDesk.Desktop.Views;

namespace StoreDesk.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }
        try
        {
            await using var db = DbConfig.CreateContext();
            var user = await db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == Login.Trim());
            if (user != null && BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                App.Current.Properties["CurrentUser"] = user;
                new MainWindow().Show();
                Application.Current.Windows.OfType<LoginWindow>().First().Close();
            }
            else
                ErrorMessage = "Неверный логин или пароль";
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("does not exist"))
                ErrorMessage = "База данных не найдена. Создайте БД в pgAdmin и выполните скрипт из папки Database (Создание_БД_Канцтовары.sql).";
            else if (msg.Contains("connection refused") || msg.Contains("actively refused"))
                ErrorMessage = "PostgreSQL не запущен. Запустите службу PostgreSQL и повторите попытку.";
            else if (msg.Contains("password") || msg.Contains("authentication"))
                ErrorMessage = "Ошибка входа в БД. Задайте пароль: переменная окружения STOREDESK_PASSWORD или укажите в Data/DbConfig.cs.";
            else
                ErrorMessage = "Ошибка БД: " + msg;
        }
    }

    [RelayCommand]
    private async Task GuestLoginAsync()
    {
        try
        {
            await using var db = DbConfig.CreateContext();
            var guest = await db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.RoleId == 3);
            if (guest != null)
            {
                App.Current.Properties["CurrentUser"] = guest;
                new MainWindow().Show();
                Application.Current.Windows.OfType<LoginWindow>().First().Close();
            }
            else
                ErrorMessage = "Учётная запись гостя не найдена в БД. Выполните скрипт Создание_БД_Канцтовары.sql.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Ошибка: " + (ex.InnerException?.Message ?? ex.Message);
        }
    }
}
