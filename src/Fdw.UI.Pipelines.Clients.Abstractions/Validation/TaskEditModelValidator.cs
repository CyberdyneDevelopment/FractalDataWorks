using FluentValidation;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Pipelines.Clients.Validation;

/// <summary>
/// Validator for TaskEditModel.
/// </summary>
public class TaskEditModelValidator : AbstractValidator<TaskEditModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskEditModelValidator"/> class.
    /// </summary>
    public TaskEditModelValidator()
    {
        RuleFor(x => x.TaskType)
            .NotEmpty().WithMessage("Task type is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required")
            .Length(1, 128).WithMessage("Task name must be 1-128 characters");

        RuleFor(x => x.InputPorts)
            .GreaterThanOrEqualTo(0).WithMessage("Input ports cannot be negative");

        RuleFor(x => x.OutputPorts)
            .GreaterThanOrEqualTo(0).WithMessage("Output ports cannot be negative");
    }
}
