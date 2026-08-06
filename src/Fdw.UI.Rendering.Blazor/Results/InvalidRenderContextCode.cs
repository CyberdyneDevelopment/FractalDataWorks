using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.UI.Rendering.Blazor.Results;

/// <summary>
/// Invalid render context, expected BlazorRenderContext.
/// </summary>
[TypeOption(typeof(BlazorUIResultCodes), "InvalidRenderContext", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidRenderContextCode : BlazorUIResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRenderContextCode"/> class.
    /// </summary>
    public InvalidRenderContextCode()
        : base(20001, "InvalidRenderContext",
            ResultSeverities.ByName("Error"),
            "Invalid render context. Expected BlazorRenderContext.",
            isRetryable: false)
    {
    }
}
