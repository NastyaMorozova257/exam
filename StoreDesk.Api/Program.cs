using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreDesk.Api.Data;
using System.Text.Json.Serialization;
using Npgsql;

// Генерация хешей паролей для сида: запуск с аргументом "hash" (dotnet run -- hash)
if (args.Length > 0 && args[0] == "hash")
{
    Console.WriteLine("P@ssChief1: " + BCrypt.Net.BCrypt.HashPassword("P@ssChief1"));
    Console.WriteLine("P@ssStaff2: " + BCrypt.Net.BCrypt.HashPassword("P@ssStaff2"));
    Console.WriteLine("P@ssAnon3: " + BCrypt.Net.BCrypt.HashPassword("P@ssAnon3"));
    Console.WriteLine("P@ssBuyer4: " + BCrypt.Net.BCrypt.HashPassword("P@ssBuyer4"));
    return;
}

var builder = WebApplication.CreateBuilder(args);
// Явно 127.0.0.1 — клиент подключается по этому адресу; иначе Kestrel может слушать только [::]:5000.
builder.WebHost.UseUrls("http://127.0.0.1:5000");

// Строка подключения к PostgreSQL: пароль из переменной POSTGRES_PASSWORD или из конфига (если БД требует пароль).
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? builder.Configuration["Postgres:Password"];
if (!string.IsNullOrEmpty(password))
{
    var csb = new NpgsqlConnectionStringBuilder(connStr) { Password = password };
    connStr = csb.ConnectionString;
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TradeDbContext>(options => options.UseNpgsql(connStr));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWpfClient", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWpfClient");
app.UseAuthorization();
app.MapControllers();

// Проверка БД при старте. В VS: Вид → Вывод → "Показать вывод из:" выберите "Отладка"
var logger = app.Services.GetRequiredService<ILogger<Program>>();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
    try
    {
        await db.Database.CanConnectAsync();
        logger.LogInformation("[StoreDesk.Api] БД подключена (PostgreSQL).");
        Console.WriteLine(">>> [StoreDesk.Api] БД подключена (PostgreSQL). <<<");
    }
    catch (Exception ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        logger.LogError("[StoreDesk.Api] Ошибка подключения к БД: {Message}", msg);
        Console.WriteLine(">>> [StoreDesk.Api] Ошибка БД: " + msg + " <<<");
        if (msg.Contains("does not exist")) Console.WriteLine(">>> Создайте базу trade_db в pgAdmin и выполните ПОЛНАЯ_УСТАНОВКА_БД.sql <<<");
        if (msg.Contains("refused") || msg.Contains("actively refused")) Console.WriteLine(">>> Запустите службу PostgreSQL (services.msc) <<<");
        if (msg.Contains("password") && msg.Contains("authentication")) Console.WriteLine(">>> Задайте пароль: переменная окружения POSTGRES_PASSWORD или в appsettings.Development.json ключ \"Postgres\": { \"Password\": \"ваш_пароль\" } <<<");
    }
}

app.Run();
