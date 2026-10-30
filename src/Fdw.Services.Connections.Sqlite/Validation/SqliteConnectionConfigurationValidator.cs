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

        RuleFor(x => x.DataSource)
            .NotEmpty()
            .WithMessage("DataSource is required");

        RuleFor(x => x.Mode)
            .NotEmpty()
            .WithMessage("Mode is required (e.g., ReadWriteCreate, ReadWrite, ReadOnly, Memory)");

        RuleFor(x => x.Cache)
            .NotEmpty()
            .WithMessage("Cache is required (e.g., Default, Shared, Private)");

        RuleFor(x => x.DefaultTimeout)
            .GreaterThan(0)
            .When(x => x.DefaultTimeout.HasValue)
            .WithMessage("DefaultTimeout must be greater than 0 when it is stated; leave it out to take the instance's DefaultCommandTimeoutSeconds");
    }
}
