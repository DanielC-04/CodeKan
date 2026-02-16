using DevBoard.Application.Auth.Dtos;
using FluentValidation;

namespace DevBoard.Api.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(item => item.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(item => item.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);
    }
}
