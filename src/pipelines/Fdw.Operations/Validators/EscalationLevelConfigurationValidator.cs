using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Fdw.Messages;
using Fdw.Validation;
using Fdw.Operations.Configuration;
using Fdw.Operations.Abstractions.TypeCollections.Escalation;
using Fdw.Operations.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="EscalationLevelConfiguration"/>. A level's severity is one of <see cref="EscalationSeverities"/>, the list the API serves to the console (FDW-4693).
/// </summary>
public sealed class EscalationLevelConfigurationValidator : FdwConfigurationValidator<EscalationLevelConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationLevelConfigurationValidator"/> class.
    /// </summary>
    public EscalationLevelConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Level must be at least 1");

        RuleFor(x => x.DelayMinutes)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DelayMinutes must be 0 or greater");

        RuleFor(x => x.NotificationChannel)
            .NotEmpty()
            .WithMessage("NotificationChannel is required");

        // Why a custom rule: each reason is its own coded message, so the failure carries that message's code and text, and the message itself in its custom state for the validator runner to pass on (one reason, one message).
        RuleFor(x => x.Severity)
            .Custom((severity, context) =>
            {
                if (string.IsNullOrWhiteSpace(severity))
                {
                    Refuse(context, EscalationValidationLog.SeverityNotStated(NullLogger.Instance));
                }
                else if (ReferenceEquals(EscalationSeverities.ByName(severity), EscalationSeverities.NotFound))
                {
                    Refuse(context, EscalationValidationLog.SeverityUnknown(NullLogger.Instance, severity, string.Join(", ", EscalationSeverities.All().Select(option => option.Name).OrderBy(name => name, StringComparer.Ordinal))));
                }
            });
    }

    private static void Refuse(ValidationContext<EscalationLevelConfiguration> context, IGenericMessage message) =>
        context.AddFailure(new ValidationFailure(nameof(EscalationLevelConfiguration.Severity), message.Message) { ErrorCode = message.Code, CustomState = message });
}
