using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.UI.Rendering.Spectre.Results;

/// <summary>
/// Invalid render context, expected SpectreRenderContext.
/// </summary>
[TypeOption(typeof(SpectreUIResultCodes), "InvalidRenderContext", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidRenderContextCode : SpectreUIResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRenderContextCode"/> class.
    /// </summary>
    public InvalidRenderContextCode()
        : base(20001, "InvalidRenderContext",
            ResultSeverities.ByName("Error"),
            "Invalid render context. Expected SpectreRenderContext.",
            isRetryable: false)
    {
    }
}