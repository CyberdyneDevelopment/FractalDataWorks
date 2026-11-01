using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Validators;

/// <summary>
/// Validator for <see cref="NotificationRecipientConfiguration"/>.
/// </summary>
public sealed class NotificationRecipientConfigurationValidator : FdwConfigurationValidator<NotificationRecipientConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRecipientConfigurationValidator"/> class.
    /// </summary>
    public NotificationRecipientConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Recipient is required");

        RuleFor(x => x.RecipientType)
            .NotEmpty()
            .WithMessage("RecipientType is required");
    }
}
