namespace backend_api_base_netcore8.Application.DTOs.Login;

public class LoginResponse
{
    public int UserId { get; set; }
    public int Status { get; set; }
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public User.UserDto User { get; set; } = new();
}
