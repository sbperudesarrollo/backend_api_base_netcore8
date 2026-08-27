using System.Text.Json.Serialization;

namespace backend_api_base_netcore8.Application.DTOs.Login;

public class GoogleTokenInfo
{
    [JsonPropertyName("aud")]
    public string Audience { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("email_verified")]
    public string EmailVerifiedRaw { get; set; } = string.Empty;

    public bool IsEmailVerified =>
        string.Equals(EmailVerifiedRaw, "true", StringComparison.OrdinalIgnoreCase);
}
