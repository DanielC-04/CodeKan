using DevBoard.Application.Projects.Dtos;
using FluentValidation;

namespace DevBoard.Api.Validators;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.RepoOwner)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.RepoName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.GitHubToken)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
