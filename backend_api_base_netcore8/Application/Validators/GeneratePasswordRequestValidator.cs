using backend_api_base_netcore8.Application.DTOs;
using FluentValidation;

namespace backend_api_base_netcore8.Application.Validators;

public class GeneratePasswordRequestValidator : AbstractValidator<GeneratePasswordRequest>
{
    private const int MinLength = 8;
    private const int MaxLength = 64;

    public GeneratePasswordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.Length)
            .InclusiveBetween(MinLength, MaxLength);
    }
}
