using backend_api_base_netcore8.Application.DTOs.Login;
using FluentValidation;

namespace backend_api_base_netcore8.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
