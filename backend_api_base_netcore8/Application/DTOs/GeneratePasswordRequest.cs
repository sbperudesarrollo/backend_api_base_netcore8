namespace backend_api_base_netcore8.Application.DTOs;

public class GeneratePasswordRequest
{
    public int UserId { get; set; }
    public int Length { get; set; } = 12;
}
