using System;
using System.Linq;
using FluentValidation;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Fdw.Validation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Scheduling.Endpoints.Validators;

/// <summary>
/// The rules a create-schedule request must meet in any host. A host derives from this class (so the endpoint scan finds a concrete
/// validator) and adds only what the host alone knows (FDW-3572).
/// </summary>
/// <remarks>
/// A schedule's name column is 200 characters, so that is the name limit; the pipeline name column is 200, the cron expression 255 and the event name 200 (FDW-3932:
/// a limit above its column is a failed insert, not a refusal); the scheduler types are the <see cref="TriggerTypes"/> collection's.
/// </remarks>
public abstract class CreateScheduleRequestValidator : FdwEndpointValidator<CreateScheduleRequest>
{
    private static readonly string MissedFireRuleNames = string.Join(", ", MissedFireRules.All().Select(rule => rule.Name));

    private static bool IsMissedFireRule(string? rule) =>
        MissedFireRules.All().Any(option => string.Equals(option.Name, rule, StringComparison.Ordinal));

    /// <summary>Initializes a new instance of the <see cref="CreateScheduleRequestValidator"/> class.</summary>
    protected CreateScheduleRequestValidator()
    {
        ValidateName(x => x.Name);

        // The pipeline is named by id, resolved by the API proxy in its caller's view (FDW-1088); the name is the
        // one it resolved, kept for display.
        RuleFor(x => x.PipelineId)
            .NotEmpty()
            .WithMessage("Pipeline id is required");

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("Pipeline name is required with the pipeline id")
            .MaximumLength(200)
            .WithMessage("Pipeline name cannot exceed 200 characters");

        RuleFor(x => x.Parameters)
            .Must(ScheduleParameterMapper.NamesAreDistinct)
            .WithMessage("Every run parameter value needs a name, and no name may be given twice");

        // What the schedule's runs act with (FDW-1161): at least one role. Whether the caller holds them is the endpoint's
        // question, because it needs the caller's roles.
        RuleFor(x => x.RunRoles)
            .NotEmpty()
            .WithMessage("RunRoles is required: name at least one role the schedule's runs act with");

        // Required, with no default (FDW-3788): what the scheduler does with a cron fire time that passed while it was down. The names are read from the collection.
        RuleFor(x => x.MissedFireRule)
            .NotEmpty()
            .WithMessage($"MissedFireRule is required: {MissedFireRuleNames}")
            .Must(IsMissedFireRule)
            .WithMessage($"MissedFireRule must be one of: {MissedFireRuleNames}");

        RuleFor(x => x.SchedulerType)
            .NotEmpty()
            .WithMessage("Scheduler type is required (Cron, Interval, Manual, OneTime, or Event)")
            .Must(IsSchedulerType)
            .WithMessage("Scheduler type must be one of: Cron, Interval, Manual, OneTime, Event");

        When(x => string.Equals(x.SchedulerType, "Cron", StringComparison.OrdinalIgnoreCase), () =>
        {
            // Stop at the first failure: the shared cron rule repeats the not-empty check, and an empty expression should answer once.
            RuleFor(x => x.CronExpression!)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Cron expression is required for Cron scheduler type")
                .MaximumLength(255)
                .WithMessage("Cron expression cannot exceed 255 characters")
                .IsValidCronExpression();
        });

        When(x => string.Equals(x.SchedulerType, "Interval", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.IntervalSeconds)
                .NotNull()
                .WithMessage("Interval seconds is required for Interval scheduler type")
                .GreaterThan(0)
                .WithMessage("Interval seconds must be greater than 0")
                .LessThanOrEqualTo(86400)
                .WithMessage("Interval seconds cannot exceed 86400 (24 hours)");
        });

        // Why: the one-time trigger type requires its date and time, so creation fails loudly without it instead of persisting an
        // unschedulable row.
        When(x => string.Equals(x.SchedulerType, "Once", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.OneTimeDateTime)
                .NotNull()
                .WithMessage("One-time date/time is required for OneTime scheduler type");
        });

        // Why: the event trigger type requires its event name.
        When(x => string.Equals(x.SchedulerType, "Event", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.EventName)
                .NotEmpty()
                .WithMessage("Event name is required for Event scheduler type")
                .MaximumLength(200)
                .WithMessage("Event name cannot exceed 200 characters");
        });
    }

    /// <summary>Whether <paramref name="type"/> names a trigger type the <see cref="TriggerTypes"/> collection holds.</summary>
    internal static bool IsSchedulerType(string? type) =>
        TriggerTypes.All().Any(trigger => string.Equals(trigger.Name, type, StringComparison.OrdinalIgnoreCase));
}
