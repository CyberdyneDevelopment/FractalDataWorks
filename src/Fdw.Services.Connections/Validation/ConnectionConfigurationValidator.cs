using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="ConnectionConfiguration"/>.
/// </summary>
public sealed class ConnectionConfigurationValidator : FdwConfigurationValidator<ConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfigurationValidator"/> class.
    /// </summary>
    public ConnectionConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.ServiceType)
            .NotEmpty()
            .WithMessage("ServiceType is required");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
