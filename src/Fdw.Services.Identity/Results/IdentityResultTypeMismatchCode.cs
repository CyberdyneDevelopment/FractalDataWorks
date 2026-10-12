using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The generic Execute surface was asked for a type this domain does not return.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityResultTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityResultTypeMismatchCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityResultTypeMismatchCode"/> class.</summary>
    public IdentityResultTypeMismatchCode()
        : base(
            91002,
            "IdentityResultTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Identity token requested as '{requestedType}' but this service returns '{actualType}'.",
            isRetryable: false)
    { }
}
