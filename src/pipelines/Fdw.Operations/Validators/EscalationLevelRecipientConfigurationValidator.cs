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
/// Validator for <see cref="EscalationLevelRecipientConfiguration"/>. A recipient's type is one of <see cref="EscalationRecipientTypes"/>, the list the API serves to the console (FDW-4693).
/// </summary>
public sealed class EscalationLevelRecipientConfigurationValidator : FdwConfigurationValidator<EscalationLevelRecipientConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationLevelRecipientConfigurationValidator"/> class.
    /// </summary>
    public EscalationLevelRecipientConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Recipient is required");

        // Why a custom rule: each reason is its own coded message, so the failure carries that message's code and text, and the message itself in its custom state for the validator runner to pass on (one reason, one message).
        RuleFor(x => x.RecipientType)
            .Custom((recipientType, context) =>
            {
                if (string.IsNullOrWhiteSpace(recipientType))
                {
                    Refuse(context, EscalationValidationLog.RecipientTypeNotStated(NullLogger.Instance));
                }
                else if (ReferenceEquals(EscalationRecipientTypes.ByName(recipientType), EscalationRecipientTypes.NotFound))
                {
                    Refuse(context, EscalationValidationLog.RecipientTypeUnknown(NullLogger.Instance, recipientType, string.Join(", ", EscalationRecipientTypes.All().Select(option => option.Name).OrderBy(name => name, StringComparer.Ordinal))));
                }
            });
    }

    private static void Refuse(ValidationContext<EscalationLevelRecipientConfiguration> context, IGenericMessage message) =>
        context.AddFailure(new ValidationFailure(nameof(EscalationLevelRecipientConfiguration.RecipientType), message.Message) { ErrorCode = message.Code, CustomState = message });
}
