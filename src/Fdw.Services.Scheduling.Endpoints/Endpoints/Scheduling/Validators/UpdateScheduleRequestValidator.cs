using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Scheduling.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="UpdateScheduleRequest"/>.
/// </summary>
public abstract class UpdateScheduleRequestValidator : FdwEndpointValidator<UpdateScheduleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateScheduleRequestValidator"/> class.
    /// </summary>
    protected UpdateScheduleRequestValidator()
    {
        ValidateName(x => x.Name);

        RuleFor(x => x)
            .Must(x => x.PipelineName is not null
                || x.SchedulerType is not null
                || x.CronExpression is not null
                || x.IntervalSeconds is not null
                || x.IsEnabled is not null)
            .WithMessage("At least one updateable field must be supplied");

        When(x => x.PipelineName is not null, () =>
        {
            RuleFor(x => x.PipelineName)
                .NotEmpty()
                .WithMessage("PipelineName cannot be empty when provided");
        });

        When(x => x.SchedulerType is not null, () =>
        {
            RuleFor(x => x.SchedulerType)
                .NotEmpty()
                .WithMessage("SchedulerType cannot be empty when provided");
        });

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
