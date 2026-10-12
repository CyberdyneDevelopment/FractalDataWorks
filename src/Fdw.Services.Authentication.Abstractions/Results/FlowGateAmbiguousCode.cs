using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The flow requires several permissions, so no single one can be set or removed.
/// </summary>
/// <remarks>
/// Several gate steps is a deliberate configuration (a principal must hold every permission named). A console that sets one permission would silently collapse it, so the change is refused and left to whoever wrote it. An endpoint answers it with a 409.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "FlowGateAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FlowGateAmbiguousCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlowGateAmbiguousCode"/> class.</summary>
    public FlowGateAmbiguousCode()
        : base(41001, "FlowGateAmbiguous",
            ResultSeverities.ByName("Error"),
            "The flow requires several permissions; change it in configuration",
            isRetryable: false)
    {
    }
}
