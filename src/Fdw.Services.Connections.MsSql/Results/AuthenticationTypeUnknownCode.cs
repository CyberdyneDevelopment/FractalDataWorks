using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// The connection's AuthenticationType does not match any registered MsSql authentication type.
/// </summary>
// Why: ByName returned the NotFound sentinel — an unrecognised or unset discriminator on the configuration row.
[TypeOption(typeof(MsSqlConnectionResultCodes), "AuthenticationTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AuthenticationTypeUnknownCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationTypeUnknownCode"/> class.
    /// </summary>
    public AuthenticationTypeUnknownCode()
        : base(
            60014,
            "AuthenticationTypeUnknown",
            ResultSeverities.ByName("Error"),
            "The connection's AuthenticationType does not match any registered MsSql authentication type.")
    {
    }
}
