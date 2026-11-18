using Fdw.Validation;
using FluentValidation;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="DataStoreWorkingPathConfiguration"/> (FDW-6394): a name is not blank and fits the column that holds it.
/// </summary>
public sealed class DataStoreWorkingPathConfigurationValidator : FdwConfigurationValidator<DataStoreWorkingPathConfiguration>
{
    // As wide as the data path name it is compared with (data.DataPath.Name).
    private const int NameLength = 500;

    /// <summary>Initializes a new instance of the <see cref="DataStoreWorkingPathConfigurationValidator"/> class.</summary>
    public DataStoreWorkingPathConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("A working area needs a name")
            .MaximumLength(NameLength);
    }
}
