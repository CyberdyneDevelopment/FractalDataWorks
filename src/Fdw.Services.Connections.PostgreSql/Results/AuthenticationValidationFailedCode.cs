using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.PostgreSql.Results;

/// <summary>
/// Authentication validation failed for PostgreSQL connection.
/// </summary>
[TypeOption(typeof(PostgreSqlResultCodes), "AuthenticationValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AuthenticationValidationFailedCode : PostgreSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationValidationFailedCode"/> class.
    /// </summary>
    public AuthenticationValidationFailedCode()
        : base(
            50000,
            "AuthenticationValidationFailed",
            ResultSeverities.ByName("Error"),
            "PostgreSQL authentication validation failed: {ValidationErrors}",
            isRetryable: false)
    {
    }
}
