using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The header loaded without its typed body, so no mechanism can be constructed.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityTypedBodyMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityTypedBodyMissingCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityTypedBodyMissingCode"/> class.</summary>
    public IdentityTypedBodyMissingCode()
        : base(
            61001,
            "IdentityTypedBodyMissing",
            ResultSeverities.ByName("Critical"),
            "Identity configuration '{configurationName}' declares mechanism '{mechanism}' but its typed body did not load.",
            isRetryable: false)
    { }
}
