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
            .Must(x => UpdateScheduleChanges.Stated(x))
            .WithErrorCode(UpdateScheduleChanges.NothingStatedCode)
            .WithMessage(UpdateScheduleChanges.RefusalMessage);

        When(x => x.PipelineId is not null, () =>
        {
            RuleFor(x => x.PipelineId)
                .NotEmpty()
                .WithMessage("PipelineId cannot be empty when provided");
            RuleFor(x => x.PipelineName)
                .NotEmpty()
                .WithMessage("PipelineName is required with PipelineId");
        });

        When(x => x.PipelineId is null, () =>
        {
            RuleFor(x => x.PipelineName)
                .Null()
                .WithMessage("PipelineName is sent only with PipelineId");
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
