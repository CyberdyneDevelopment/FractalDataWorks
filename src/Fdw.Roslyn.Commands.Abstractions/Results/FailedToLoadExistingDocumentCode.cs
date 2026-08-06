using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to load existing document.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToLoadExistingDocument", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToLoadExistingDocumentCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToLoadExistingDocumentCode"/> class.
    /// </summary>
    public FailedToLoadExistingDocumentCode()
        : base(91010, "FailedToLoadExistingDocument",
            ResultSeverities.ByName("Error"),
            "Failed to load existing document",
            isRetryable: false)
    {
    }
}
