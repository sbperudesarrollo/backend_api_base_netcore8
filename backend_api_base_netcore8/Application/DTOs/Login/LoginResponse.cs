namespace backend_api_base_netcore8.Application.DTOs.Login;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    //public UserDto User { get; set; } = new();
}
