using Microsoft.EntityFrameworkCore;
using StoreDesk.Data;

namespace StoreDesk.Desktop.Data;

/// <summary>Подключение к PostgreSQL (pgAdmin). Пароль можно задать переменной STOREDESK_PASSWORD или оставить пустым.</summary>
public static class DbConfig
{
    public static string ConnectionString
    {
        get
        {
            var pass = Environment.GetEnvironmentVariable("STOREDESK_PASSWORD") ?? "";
            var baseStr = "Host=localhost;Port=5432;Database=trade_db;Username=postgres;Password=";
            if (string.IsNullOrEmpty(pass))
                return baseStr + ";";
            return baseStr + pass + ";";
        }
    }

    public static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    public static AppDbContext CreateContext() => new AppDbContext(CreateOptions());
}
