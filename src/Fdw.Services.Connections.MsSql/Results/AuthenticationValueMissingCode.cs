using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// A required authentication property is absent or empty. The authentication type declares it as required, so the connection cannot be built.
/// </summary>
// Why: the auth KVP bag is a raw dictionary; a bool+out lookup cannot say which property was missing or which auth type required it.
[TypeOption(typeof(MsSqlConnectionResultCodes), "AuthenticationValueMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AuthenticationValueMissingCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationValueMissingCode"/> class.
    /// </summary>
    public AuthenticationValueMissingCode()
        : base(
            60010,
            "AuthenticationValueMissing",
            ResultSeverities.ByName("Error"),
            "A required authentication property is absent or empty. The authentication type declares it as required, so the connection cannot be built.")
    {
    }
}
