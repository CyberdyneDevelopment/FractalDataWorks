using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.ApiReference.Results;

/// <summary>
/// A signed-in caller asked for an API reference document whose page requires a policy they do not hold.
/// </summary>
/// <remarks>
/// 100000, the Forbidden category (HTTP 403), not the Auth category, which maps to 401 and would send a
/// correctly signed-in caller back to sign in. The required policies are the page's own, or the ones
/// the host configured for it (<see cref="ApiReferencePagePolicies"/>).
/// </remarks>
[TypeOption(typeof(ApiReferenceResultCodes), "ReferencePageNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ReferencePageNotPermittedCode : ApiReferenceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ReferencePageNotPermittedCode"/> class.</summary>
    public ReferencePageNotPermittedCode()
        : base(100000, "ReferencePageNotPermitted",
            ResultSeverities.ByName("Error"),
            "The API reference page '{page}' cannot be opened: {reason}",
            isRetryable: false)
    {
    }
}
