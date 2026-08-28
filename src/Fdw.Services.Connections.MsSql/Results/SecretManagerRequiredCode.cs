using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// This authentication type requires a secret, but no secret manager was supplied. Build the connection through the connection provider, or pass a secret manager explicitly.
/// </summary>
[TypeOption(typeof(MsSqlConnectionResultCodes), "SecretManagerRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretManagerRequiredCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerRequiredCode"/> class.
    /// </summary>
    public SecretManagerRequiredCode()
        : base(
            60011,
            "SecretManagerRequired",
            ResultSeverities.ByName("Error"),
            "This authentication type requires a secret, but no secret manager was supplied. Build the connection through the connection provider, or pass a secret manager explicitly.")
    {
    }
}
