using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The configuration names a mechanism no option provides.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityMechanismNotRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityMechanismNotRegisteredCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityMechanismNotRegisteredCode"/> class.</summary>
    public IdentityMechanismNotRegisteredCode()
        : base(
            61003,
            "IdentityMechanismNotRegistered",
            ResultSeverities.ByName("Critical"),
            "Identity mechanism '{requested}' is not registered.",
            isRetryable: false)
    { }
}
