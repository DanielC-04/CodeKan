using DevBoard.Application.Tasks.Dtos;
using FluentValidation;

namespace DevBoard.Api.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(request => request.Description)
            .MaximumLength(20000);
    }
}
