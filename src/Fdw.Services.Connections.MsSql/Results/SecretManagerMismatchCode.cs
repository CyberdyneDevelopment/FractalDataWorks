using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// The supplied secret manager is not the one this connection declares. Its SecretManagerName does not match.
/// </summary>
// Why: silently resolving a secret from a manager the connection did not name would read the wrong store.
[TypeOption(typeof(MsSqlConnectionResultCodes), "SecretManagerMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretManagerMismatchCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerMismatchCode"/> class.
    /// </summary>
    public SecretManagerMismatchCode()
        : base(
            60012,
            "SecretManagerMismatch",
            ResultSeverities.ByName("Error"),
            "The supplied secret manager is not the one this connection declares. Its SecretManagerName does not match.")
    {
    }
}
