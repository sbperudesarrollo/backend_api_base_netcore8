using backend_api_base_netcore8.Application.DTOs;
using backend_api_base_netcore8.Application.DTOs.Login;
using backend_api_base_netcore8.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace backend_api_base_netcore8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<GeneratePasswordRequest> _passwordGeneratorValidator;

    public AuthController(
        IAuthService authService,
        IPasswordService passwordService,
        IValidator<LoginRequest> loginValidator,
        IValidator<GeneratePasswordRequest> passwordGeneratorValidator)
    {
        _authService = authService;
        _passwordService = passwordService;
        _loginValidator = loginValidator;
        _passwordGeneratorValidator = passwordGeneratorValidator;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var loginResponse = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        if (loginResponse is null)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        return Ok(loginResponse);
    }

    [HttpPost("password")]
    [ProducesResponseType(typeof(PasswordGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePassword([FromBody] GeneratePasswordRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _passwordGeneratorValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var response = await _passwordService.GenerateAndUpdatePasswordAsync(request, cancellationToken);
        if (response is null)
        {
            return NotFound(new { error = "User not found or password could not be updated" });
        }

        return Ok(response);
    }
}
