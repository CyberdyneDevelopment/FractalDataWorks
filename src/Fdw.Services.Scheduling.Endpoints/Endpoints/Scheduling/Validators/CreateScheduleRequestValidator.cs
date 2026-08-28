using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Scheduling.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateScheduleRequest"/>.
/// </summary>
public abstract class CreateScheduleRequestValidator : FdwEndpointValidator<CreateScheduleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateScheduleRequestValidator"/> class.
    /// </summary>
    protected CreateScheduleRequestValidator()
    {
        ValidateName(x => x.Name);

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("PipelineName is required");

        RuleFor(x => x.SchedulerType)
            .NotEmpty()
            .WithMessage("SchedulerType is required");

        When(x => string.Equals(x.SchedulerType, "Cron", System.StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CronExpression)
                .NotEmpty()
                .WithMessage("CronExpression is required for Cron schedules");
        });

        When(x => string.Equals(x.SchedulerType, "Interval", System.StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.IntervalSeconds)
                .NotNull()
                .WithMessage("IntervalSeconds is required for Interval schedules")
                .GreaterThanOrEqualTo(1)
                .WithMessage("IntervalSeconds must be at least 1");
        });
    }
}
