namespace backend_api_base_netcore8.Application.DTOs.Clientes;

public class UpdateClienteRequest
{
    public string Code { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
