namespace backend_api_base_netcore8.Domain.Entities;

/// <summary>
/// Represents a user record mapped 1:1 with the `users` table.
/// </summary>
public class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Name { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int? DegreeId { get; set; }
    public string? RememberToken { get; set; }
    public long? Phone { get; set; }
    public long? Cip { get; set; }
}
