using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The federated assertion existed but could not be read.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityAssertionUnreadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityAssertionUnreadableCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityAssertionUnreadableCode"/> class.</summary>
    public IdentityAssertionUnreadableCode()
        : base(
            71002,
            "IdentityAssertionUnreadable",
            ResultSeverities.ByName("Error"),
            "Could not read the federated assertion at '{location}'.",
            isRetryable: false)
    { }
}
