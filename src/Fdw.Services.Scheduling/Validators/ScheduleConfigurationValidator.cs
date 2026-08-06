using FluentValidation;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Scheduling.Validators;

/// <summary>
/// Validator for <see cref="ScheduleConfiguration"/>.
/// </summary>
public sealed class ScheduleConfigurationValidator : FdwConfigurationValidator<ScheduleConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfigurationValidator"/> class.
    /// </summary>
    public ScheduleConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("PipelineName is required");

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxRetries must be 0 or greater");

        RuleFor(x => x.RetryDelaySeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("RetryDelaySeconds must be 0 or greater");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThanOrEqualTo(1)
            .WithMessage("TimeoutSeconds must be at least 1");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
