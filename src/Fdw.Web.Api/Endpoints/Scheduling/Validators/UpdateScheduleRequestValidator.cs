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
        // Why: PUT here is a partial update — clients may send just {isEnabled:false} to toggle
        // a single field. Required-on-update validation rejected those legitimate PATCH-style
        // bodies with 400. Only enforce required fields when the client supplied them.
        ValidateName(x => x.Name);

        // Why: reject bodies that supply no updateable field. A PUT with only {name:...}
        // (which always matches the path) is a no-op — Newman expects 400 here. At least
        // one of PipelineName/SchedulerType/CronExpression/IntervalSeconds/IsEnabled must
        // be present.
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
