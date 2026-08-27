using backend_api_base_netcore8.Application.DTOs.Common;
using backend_api_base_netcore8.Application.DTOs.Roles;
using backend_api_base_netcore8.Application.DTOs.Users;
using backend_api_base_netcore8.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api_base_netcore8.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CollectionResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int perPage = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _usersService.GetAllAsync(page, perPage, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                MessageError = ex.Message
            });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await _usersService.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await _usersService.GetRolesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        await _usersService.CreateAsync(request, cancellationToken);
        return Ok("Usuario creado.");
    }

    [HttpPut]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await _usersService.UpdateAsync(request.Id, request, cancellationToken);
        return response is null ? NotFound() : Ok("Usuario actualizado.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _usersService.DeleteAsync(id, cancellationToken);
        return deleted ? Ok("Usuario eliminado.") : NotFound();
    }
}
