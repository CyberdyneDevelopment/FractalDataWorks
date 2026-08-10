using FluentValidation;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Pipelines.Clients.Models.Validation;

/// <summary>
/// Validator for PipelineEditModel.
/// </summary>
public class PipelineEditModelValidator : AbstractValidator<PipelineEditModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineEditModelValidator"/> class.
    /// </summary>
    public PipelineEditModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(1, 128).WithMessage("Name must be 1-128 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or less");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid PipelineStatus value");

        RuleForEach(x => x.Tasks)
            .SetValidator(new TaskEditModelValidator());
    }
}
