using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="DataPathConfiguration"/>.
/// </summary>
public sealed class DataPathConfigurationValidator : FdwConfigurationValidator<DataPathConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPathConfigurationValidator"/> class.
    /// </summary>
    public DataPathConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.DataStoreId)
            .NotEqual(System.Guid.Empty)
            .WithMessage("DataStoreId is required");

        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("Path is required")
            .IsSafeString(512);

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
