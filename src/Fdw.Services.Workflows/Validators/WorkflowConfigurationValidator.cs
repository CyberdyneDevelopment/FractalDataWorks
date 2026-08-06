using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Workflows.Validators;

/// <summary>
/// Validator for <see cref="WorkflowConfiguration"/>.
/// </summary>
public sealed class WorkflowConfigurationValidator : FdwConfigurationValidator<WorkflowConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConfigurationValidator"/> class.
    /// </summary>
    public WorkflowConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.MaxConcurrentExecutions)
            .GreaterThanOrEqualTo(1)
            .WithMessage("MaxConcurrentExecutions must be at least 1");

        RuleFor(x => x.DefaultExecutionTimeout)
            .Must(t => t > System.TimeSpan.Zero)
            .WithMessage("DefaultExecutionTimeout must be a positive value");

        When(x => x.TimeoutSeconds.HasValue, () =>
        {
            RuleFor(x => x.TimeoutSeconds!.Value)
                .GreaterThanOrEqualTo(1)
                .WithMessage("TimeoutSeconds must be at least 1");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
