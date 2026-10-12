using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Results;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// How the flow administration routes read a refused change.
/// </summary>
/// <remarks>
/// A flow nobody configured answers 404, which the result-to-status mapping deliberately leaves to the
/// route. Every other refusal carries a coded category (400, 409) the mapping already answers.
/// </remarks>
public static class AuthenticationFlowFailure
{
    /// <summary>Gets whether the failure is "no such flow".</summary>
    /// <param name="result">The failed result.</param>
    /// <returns>True when the route should answer 404.</returns>
    public static bool IsNotFound(IGenericResult result)
        => result.CodeChain.Contains(AuthenticationResultCodes.ByName("FlowNotFound"));
}
