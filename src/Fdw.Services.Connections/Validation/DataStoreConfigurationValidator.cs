using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="DataStoreConfiguration"/>.
/// </summary>
public sealed class DataStoreConfigurationValidator : FdwConfigurationValidator<DataStoreConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreConfigurationValidator"/> class.
    /// </summary>
    public DataStoreConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.ConnectionId)
            .IsNotEmpty();

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
