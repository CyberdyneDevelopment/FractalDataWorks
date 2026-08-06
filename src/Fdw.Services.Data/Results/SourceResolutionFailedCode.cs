using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Failed to resolve sources.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "SourceResolutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceResolutionFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceResolutionFailedCode"/> class.
    /// </summary>
    public SourceResolutionFailedCode()
        : base(91005, "SourceResolutionFailed", ResultSeverities.ByName("Error"),
            "Failed to resolve sources",
            isRetryable: false)
    {
    }
}