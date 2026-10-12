using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.PostgreSql.Results;

/// <summary>
/// An authentication type declared a secret-bearing key (e.g. SecretKeyName) but no
/// SecretManagerName to resolve it from.
/// </summary>
[TypeOption(typeof(PostgreSqlResultCodes), "SecretManagerNameMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretManagerNameMissingCode : PostgreSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerNameMissingCode"/> class.
    /// </summary>
    public SecretManagerNameMissingCode()
        : base(
            50001,
            "SecretManagerNameMissing",
            ResultSeverities.ByName("Error"),
            "{AuthenticationType} authentication declares a secret-bearing key but no SecretManagerName to resolve it from",
            isRetryable: false)
    {
    }
}
