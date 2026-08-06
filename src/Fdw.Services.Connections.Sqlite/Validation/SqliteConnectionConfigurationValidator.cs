using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Sqlite.Validation;

/// <summary>
/// Validator for <see cref="SqliteConnectionConfiguration"/>.
/// </summary>
public sealed class SqliteConnectionConfigurationValidator : FdwConfigurationValidator<SqliteConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionConfigurationValidator"/> class.
    /// </summary>
    public SqliteConnectionConfigurationValidator()
    {
        // Why: Name is a header field on ConnectionConfiguration after config-split.
        // SqliteConnectionConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        RuleFor(x => x.DataSource)
            .NotEmpty()
            .WithMessage("DataSource is required");

        RuleFor(x => x.Mode)
            .NotEmpty()
            .WithMessage("Mode is required (e.g., ReadWriteCreate, ReadWrite, ReadOnly, Memory)");

        RuleFor(x => x.Cache)
            .NotEmpty()
            .WithMessage("Cache is required (e.g., Default, Shared, Private)");
    }
}
