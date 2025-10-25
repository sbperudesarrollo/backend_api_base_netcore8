namespace backend_api_base_netcore8.Application.DTOs;

public class PasswordGenerationResponse
{
    public int UserId { get; set; }
    public string Password { get; set; } = string.Empty;
}
