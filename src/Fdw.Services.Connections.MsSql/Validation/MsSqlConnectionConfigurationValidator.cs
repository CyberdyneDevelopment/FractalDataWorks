using FluentValidation;
using Fdw.Services.Connections;
using Fdw.Validation;

namespace Fdw.Services.Connections.MsSql.Validation;

/// <summary>
/// Validator for <see cref="MsSqlConnectionConfiguration"/>.
/// </summary>
public sealed class MsSqlConnectionConfigurationValidator : FdwConfigurationValidator<MsSqlConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionConfigurationValidator"/> class.
    /// </summary>
    public MsSqlConnectionConfigurationValidator()
    {
        // Why: Name is a header field on ConnectionConfiguration after config-split.
        // MsSqlConnectionConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        // SQL Server specific
        RuleFor(x => x.Server)
            .NotEmpty()
            .WithMessage("Server is required");

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage("Database is required");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535");

        RuleFor(x => x.CommandTimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("CommandTimeoutSeconds must be greater than 0");

        RuleFor(x => x.ConnectionTimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("ConnectionTimeoutSeconds must be greater than 0");

        When(x => x.EnableConnectionPooling, () =>
        {
            RuleFor(x => x.MaxPoolSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage("MaxPoolSize must be at least 1 when connection pooling is enabled");

            RuleFor(x => x.MinPoolSize)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MinPoolSize must be greater than or equal to 0");

            RuleFor(x => x.MinPoolSize)
                .LessThanOrEqualTo(x => x.MaxPoolSize)
                .WithMessage("MinPoolSize must not exceed MaxPoolSize");
        });

        RuleFor(x => x.DefaultSchema)
            .NotEmpty()
            .WithMessage("DefaultSchema is required");
    }
}
