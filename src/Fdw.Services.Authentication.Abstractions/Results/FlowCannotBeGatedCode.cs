using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The flow cannot be made to require a permission.
/// </summary>
/// <remarks>
/// A state of the flow, not a fault of the host: no step in it establishes what a permission gate reads, so there is nowhere a gate could run. An endpoint answers it with a 409.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "FlowCannotBeGated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FlowCannotBeGatedCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlowCannotBeGatedCode"/> class.</summary>
    public FlowCannotBeGatedCode()
        : base(41000, "FlowCannotBeGated",
            ResultSeverities.ByName("Error"),
            "The flow establishes nothing a permission gate could read",
            isRetryable: false)
    {
    }
}
