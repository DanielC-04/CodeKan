using DevBoard.Application.Tasks.Dtos;
using FluentValidation;

namespace DevBoard.Api.Validators;

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    private static readonly string[] AllowedStatuses = ["Todo", "InProgress", "Done"];

    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(request => request.Status)
            .NotEmpty()
            .Must(status => AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be one of: Todo, InProgress, Done.");
    }
}
