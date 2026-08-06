using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Notifications.Validators;

/// <summary>
/// Validator for <see cref="NotificationConfiguration"/>.
/// </summary>
public sealed class NotificationConfigurationValidator : FdwConfigurationValidator<NotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConfigurationValidator"/> class.
    /// </summary>
    public NotificationConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");
    }
}
