using DevBoard.Application.Auth.Dtos;
using FluentValidation;

namespace DevBoard.Api.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(item => item.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(item => item.Password)
            .NotEmpty()
            .MaximumLength(100);
    }
}
