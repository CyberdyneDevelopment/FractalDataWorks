using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// No sign-in flow of the requested name is configured on this host.
/// </summary>
/// <remarks>
/// A caller's mistake rather than a fault of the host, so an endpoint answers it with a 4xx. Kept
/// apart from a flow that is configured but invalid, which is the host's fault.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "FlowNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FlowNotFoundCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlowNotFoundCode"/> class.</summary>
    public FlowNotFoundCode()
        : base(30000, "FlowNotFound",
            ResultSeverities.ByName("Error"),
            "No sign-in flow of that name is configured",
            isRetryable: false)
    {
    }
}
