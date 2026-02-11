using System.Text.Json.Serialization;

namespace StoreDesk.Desktop.Models;

public class User
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("roleId")]
    public int RoleId { get; set; }
    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;
}
