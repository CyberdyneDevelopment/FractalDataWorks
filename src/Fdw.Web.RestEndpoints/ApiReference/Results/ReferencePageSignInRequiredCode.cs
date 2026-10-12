using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.ApiReference.Results;

/// <summary>
/// An anonymous caller asked for an API reference document whose page requires a signed-in caller.
/// </summary>
/// <remarks>
/// 50000, the catalog's authentication failure (category 5, HTTP 401): the caller should sign in and
/// ask again.
/// </remarks>
[TypeOption(typeof(ApiReferenceResultCodes), "ReferencePageSignInRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ReferencePageSignInRequiredCode : ApiReferenceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ReferencePageSignInRequiredCode"/> class.</summary>
    public ReferencePageSignInRequiredCode()
        : base(50000, "ReferencePageSignInRequired",
            ResultSeverities.ByName("Error"),
            "The API reference page '{page}' cannot be opened: {reason}",
            isRetryable: false)
    {
    }
}
