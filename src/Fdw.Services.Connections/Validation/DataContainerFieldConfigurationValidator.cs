using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="DataContainerFieldConfiguration"/>.
/// </summary>
public sealed class DataContainerFieldConfigurationValidator : FdwConfigurationValidator<DataContainerFieldConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainerFieldConfigurationValidator"/> class.
    /// </summary>
    public DataContainerFieldConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.DataContainerId)
            .NotEqual(System.Guid.Empty)
            .WithMessage("DataContainerId is required");


        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
