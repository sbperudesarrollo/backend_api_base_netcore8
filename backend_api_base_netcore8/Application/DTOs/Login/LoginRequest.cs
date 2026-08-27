namespace backend_api_base_netcore8.Application.DTOs.Login;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
