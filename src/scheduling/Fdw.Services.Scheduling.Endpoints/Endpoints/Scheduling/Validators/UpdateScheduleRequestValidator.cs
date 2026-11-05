using System;
using System.Linq;
using FluentValidation;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Scheduling.Endpoints.Validators;

/// <summary>
/// The rules an update-schedule request must meet in any host. A host derives from this class and adds only what the host alone knows (FDW-3572).
/// </summary>
public abstract class UpdateScheduleRequestValidator : FdwEndpointValidator<UpdateScheduleRequest>
{
    private static readonly string MissedFireRuleNames = string.Join(", ", MissedFireRules.All().Select(rule => rule.Name));

    private static bool IsMissedFireRule(string? rule) =>
        MissedFireRules.All().Any(option => string.Equals(option.Name, rule, StringComparison.Ordinal));

    /// <summary>Initializes a new instance of the <see cref="UpdateScheduleRequestValidator"/> class.</summary>
    protected UpdateScheduleRequestValidator()
    {
        // Why NotEmpty only: PUT is partial, so a field is checked only when the caller supplied it; and the name is the route's, naming a
        // schedule that exists, so a name rule here would refuse an update to a stored name written in another form.
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Schedule name is required in the route");

        // Why: a body with no updateable field is a no-op ({"name":"x"} alone; the path supplies the identifier). The members that count, and the
        // message that names them, are UpdateScheduleChanges' one list (FDW-1170).
        RuleFor(x => x)
            .Must(x => UpdateScheduleChanges.Stated(x))
            .WithErrorCode(UpdateScheduleChanges.NothingStatedCode)
            .WithMessage(UpdateScheduleChanges.RefusalMessage);

        RuleFor(x => x.Parameters)
            .Must(ScheduleParameterMapper.NamesAreDistinct)
            .WithMessage("Every run parameter value needs a name, and no name may be given twice");

        // Stated, the rule must be one of the collection's, naming the field; left out, the stored rule is kept (FDW-3788).
        When(x => x.MissedFireRule is not null, () =>
            RuleFor(x => x.MissedFireRule)
                .Must(IsMissedFireRule)
                .WithMessage($"MissedFireRule must be one of: {MissedFireRuleNames}"));

        // Stated roles replace the recorded ones, so a list that names none is refused; leaving the field out keeps them (FDW-1161).
        When(x => x.RunRoles is not null, () =>
            RuleFor(x => x.RunRoles)
                .NotEmpty()
                .WithMessage("RunRoles, when stated, names at least one role the schedule's runs act with"));

        When(x => x.PipelineId is not null, () =>
        {
            RuleFor(x => x.PipelineId)
                .NotEmpty()
                .WithMessage("Pipeline id cannot be empty when provided");
            RuleFor(x => x.PipelineName)
                .NotEmpty()
                .WithMessage("Pipeline name is required with the pipeline id")
                .MaximumLength(200)
                .WithMessage("Pipeline name cannot exceed 200 characters");
        });

        When(x => x.PipelineId is null, () =>
        {
            RuleFor(x => x.PipelineName)
                .Null()
                .WithMessage("Pipeline name is sent only with the pipeline id");
        });

        When(x => x.SchedulerType is not null, () =>
        {
            RuleFor(x => x.SchedulerType)
                .NotEmpty()
                .WithMessage("Scheduler type cannot be empty when provided")
                .Must(type => string.IsNullOrEmpty(type) || CreateScheduleRequestValidator.IsSchedulerType(type))
                .WithMessage("Scheduler type must be one of: Cron, Interval, Manual, OneTime, Event");
        });

        // Why no "Cron needs its expression" or "Interval needs its seconds" rule here: an update that states only the type keeps the
        // expression or seconds the schedule already holds, so requiring them would refuse a valid partial update. What is stated is
        // checked: a stated cron expression or interval must be one the scheduler can run.
        When(x => !string.IsNullOrEmpty(x.CronExpression), () =>
        {
            RuleFor(x => x.CronExpression)
                .MaximumLength(255)
                .WithMessage("Cron expression cannot exceed 255 characters");
        });

        When(x => x.IntervalSeconds.HasValue, () =>
        {
            RuleFor(x => x.IntervalSeconds!.Value)
                .GreaterThan(0)
                .WithMessage("Interval seconds must be greater than 0")
                .LessThanOrEqualTo(86400)
                .WithMessage("Interval seconds cannot exceed 86400 (24 hours)");
        });
    }
}
