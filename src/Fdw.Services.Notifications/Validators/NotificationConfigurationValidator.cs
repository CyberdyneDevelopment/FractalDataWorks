using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Validators;

/// <summary>
/// Validator for <see cref="INotificationImplementationConfiguration"/>.
/// </summary>
public sealed class NotificationConfigurationValidator : FdwConfigurationValidator<INotificationImplementationConfiguration>
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
