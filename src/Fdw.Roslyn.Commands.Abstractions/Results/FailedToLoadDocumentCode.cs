using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to load document.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToLoadDocument", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToLoadDocumentCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToLoadDocumentCode"/> class.
    /// </summary>
    public FailedToLoadDocumentCode()
        : base(91009, "FailedToLoadDocument",
            ResultSeverities.ByName("Error"),
            "Failed to load document",
            isRetryable: false)
    {
    }
}
