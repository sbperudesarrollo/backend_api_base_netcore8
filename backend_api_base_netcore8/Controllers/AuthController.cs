using backend_api_base_netcore8.Application.DTOs;
using backend_api_base_netcore8.Application.DTOs.Common;
using backend_api_base_netcore8.Application.DTOs.Login;
using backend_api_base_netcore8.Application.DTOs.User;
using backend_api_base_netcore8.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api_base_netcore8.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
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

    [HttpGet("echoping")]
    public IActionResult EchoPing()
    {
        return Ok(new
        {
            status = 0,
            message_error = "echoping HttpGet"
        });
    }

    [HttpGet("echouser")]
    public IActionResult EchoUser()
    {
        var identity = User.Identity;
        return Ok($" IPrincipal-user: {identity?.Name} - IsAuthenticated: {identity?.IsAuthenticated}");
    }

    [HttpPost("authenticate")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Authenticate([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse { Success = false, MessageError = "Body requerido." });
        }

        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                MessageError = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage))
            });
        }

        try
        {
            var loginResponse = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            if (loginResponse is null)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    Success = false,
                    MessageError = "Credenciales invalidas o acceso denegado."
                });
            }

            return Ok(new LoginResponse
            {
                UserId = loginResponse.UserId,
                Status = loginResponse.Status,
                Token = loginResponse.Token,
                ExpiresIn = loginResponse.ExpiresIn,
                User = loginResponse.User
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                MessageError = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                MessageError = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Success = false,
                MessageError = "Ocurrio un error inesperado."
            });
        }
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AuthenticateWithGoogle([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse { Success = false, MessageError = "Body requerido." });
        }

        try
        {
            var loginResponse = await _authService.LoginWithGoogleAsync(request.IdToken, cancellationToken);
            if (loginResponse is null)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    Success = false,
                    MessageError = "Usuario no autorizado para acceder con Google."
                });
            }

            return Ok(new LoginResponse
            {
                UserId = loginResponse.UserId,
                Status = loginResponse.Status,
                Token = loginResponse.Token,
                ExpiresIn = loginResponse.ExpiresIn,
                User = loginResponse.User
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                MessageError = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                MessageError = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Success = false,
                MessageError = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Success = false,
                MessageError = "Ocurrio un error inesperado."
            });
        }
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
