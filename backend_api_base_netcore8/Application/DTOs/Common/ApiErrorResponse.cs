namespace backend_api_base_netcore8.Application.DTOs.Common;

public class ApiErrorResponse
{
    public bool Success { get; set; }
    public string MessageError { get; set; } = string.Empty;
}
