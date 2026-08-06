using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Authentication validation failed for a SQLite connection.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "AuthenticationValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AuthenticationValidationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationValidationFailedCode"/> class.
    /// </summary>
    public AuthenticationValidationFailedCode()
        : base(
            50000,
            "AuthenticationValidationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite authentication validation failed: {ValidationErrors}",
            isRetryable: false)
    {
    }
}
