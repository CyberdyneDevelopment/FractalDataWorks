using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The sign-in flow exists but an operator has switched it off.
/// </summary>
/// <remarks>
/// A state of the flow, not a missing one: the row stays so it can be switched back on. An endpoint
/// answers it with a 4xx, because nothing is broken — the host is doing what it was told.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "FlowDisabled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FlowDisabledCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlowDisabledCode"/> class.</summary>
    public FlowDisabledCode()
        : base(40000, "FlowDisabled",
            ResultSeverities.ByName("Error"),
            "The sign-in flow is switched off",
            isRetryable: false)
    {
    }
}
