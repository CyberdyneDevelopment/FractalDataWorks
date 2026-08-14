using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The federated assertion this mechanism depends on was not present.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityAssertionNotAvailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityAssertionNotAvailableCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityAssertionNotAvailableCode"/> class.</summary>
    public IdentityAssertionNotAvailableCode()
        : base(
            61002,
            "IdentityAssertionNotAvailable",
            ResultSeverities.ByName("Error"),
            "No federated assertion found via {source} at '{location}'.",
            isRetryable: false)
    { }
}
