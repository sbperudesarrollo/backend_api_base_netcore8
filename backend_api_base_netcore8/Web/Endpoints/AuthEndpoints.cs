using backend_api_base_netcore8.Application.DTOs.Login;
using backend_api_base_netcore8.Application.Interfaces;

namespace backend_api_base_netcore8.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginAsync)
            .WithTags("Auth")
            .WithName("Login")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            validationErrors["Username"] = new[] { "Username is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            validationErrors["password"] = new[] { "Password is required." };
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var response = await authService.LoginAsync(request.Username, request.Password, cancellationToken);
        if (response is null)
        {
            return Results.Json(new { error = "Invalid credentials" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(response);
    }
}
