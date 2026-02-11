using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreDesk.Api.Data;
using StoreDesk.Api.Entities;

namespace StoreDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TradeDbContext _context;

    public AuthController(TradeDbContext context) => _context = context;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Login == request.Login);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный логин или пароль" });

        _context.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "Login", Entity = "User", EntityId = user.Id, Details = $"Вход {user.Login}", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        return Ok(new { userId = user.Id, login = user.Login, fullName = user.FullName, roleId = user.RoleId, roleName = user.Role.Name });
    }

    /// <summary>Вход как гость без пароля — по ТЗ «перейти на экран просмотра товаров в роли гостя».</summary>
    [HttpPost("guest")]
    public async Task<IActionResult> GuestEntry()
    {
        var guest = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role.Name == "Guest" || u.Role.Name == "Гость");
        if (guest == null)
            return NotFound(new { message = "Роль гостя не настроена в БД" });
        return Ok(new { userId = guest.Id, login = guest.Login, fullName = guest.FullName, roleId = guest.RoleId, roleName = guest.Role.Name });
    }
}

public class LoginRequest { public string Login { get; set; } = ""; public string Password { get; set; } = ""; }
