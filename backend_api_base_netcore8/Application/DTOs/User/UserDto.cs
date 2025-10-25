namespace backend_api_base_netcore8.Application.DTOs.User;

public class UserDto
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? DegreeId { get; set; }
    public long? Phone { get; set; }
    public long? Cip { get; set; }
}
