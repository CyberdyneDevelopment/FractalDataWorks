using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="DataContainerConfiguration"/>.
/// </summary>
public sealed class DataContainerConfigurationValidator : FdwConfigurationValidator<DataContainerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainerConfigurationValidator"/> class.
    /// </summary>
    public DataContainerConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.DataPathId)
            .NotEqual(System.Guid.Empty)
            .WithMessage("DataPathId is required");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
