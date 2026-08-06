using FluentValidation;
using Fdw.Validation;
using Fdw.Operations.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="EscalationPolicyConfiguration"/>.
/// </summary>
public sealed class EscalationPolicyConfigurationValidator : FdwConfigurationValidator<EscalationPolicyConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationPolicyConfigurationValidator"/> class.
    /// </summary>
    public EscalationPolicyConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        // Why: An escalation policy must be scoped to either a workflow or a schedule;
        // a policy with neither scope would match all executions indiscriminately.
        RuleFor(x => x)
            .Must(x => x.WorkflowId.HasValue || x.ScheduleId.HasValue)
            .WithMessage("Either WorkflowId or ScheduleId must be specified")
            .WithName("Scope");
    }
}
