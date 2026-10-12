using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// A required authentication property is absent or empty. The authentication type declares it as required, so the connection cannot be built.
/// </summary>
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
