using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.PostgreSql.Validation;

/// <summary>
/// Validator for <see cref="PostgreSqlConnectionConfiguration"/>.
/// </summary>
public sealed class PostgreSqlConnectionConfigurationValidator : FdwConfigurationValidator<PostgreSqlConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnectionConfigurationValidator"/> class.
    /// </summary>
    public PostgreSqlConnectionConfigurationValidator()
    {
        // Why: Name is a header field on ConnectionConfiguration after config-split.
        // PostgreSqlConnectionConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        RuleFor(x => x.Host)
            .NotEmpty()
            .WithMessage("Host is required")
            .IsSafeString(256);

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage("Database is required")
            .IsSafeString(128);

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535");

        RuleFor(x => x.CommandTimeout)
            .GreaterThan(0)
            .WithMessage("CommandTimeout must be greater than 0");

        RuleFor(x => x.ConnectionTimeout)
            .GreaterThan(0)
            .WithMessage("ConnectionTimeout must be greater than 0");

        RuleFor(x => x.DefaultSchema)
            .NotEmpty()
            .WithMessage("DefaultSchema is required")
            .IsSafeString(128);

        RuleFor(x => x.MaxPoolSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("MaxPoolSize must be at least 1");

        RuleFor(x => x.MinPoolSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinPoolSize must be 0 or greater");

        RuleFor(x => x)
            .Must(x => x.MinPoolSize <= x.MaxPoolSize)
            .WithMessage("MinPoolSize must not exceed MaxPoolSize")
            .WithName("PoolSize");

        When(x => x.ApplicationName is not null, () =>
        {
            RuleFor(x => x.ApplicationName!)
                .IsSafeString(128);
        });
    }
}
