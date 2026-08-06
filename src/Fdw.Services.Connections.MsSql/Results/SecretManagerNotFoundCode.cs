using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// The secret manager named by this connection is not registered. Cannot resolve the connection secret.
/// </summary>
// Why: the provider was asked for the declared manager by name and returned no match.
[TypeOption(typeof(MsSqlConnectionResultCodes), "SecretManagerNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretManagerNotFoundCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerNotFoundCode"/> class.
    /// </summary>
    public SecretManagerNotFoundCode()
        : base(
            60013,
            "SecretManagerNotFound",
            ResultSeverities.ByName("Error"),
            "The secret manager named by this connection is not registered. Cannot resolve the connection secret.")
    {
    }
}
