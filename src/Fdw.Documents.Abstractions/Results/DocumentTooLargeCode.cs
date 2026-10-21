using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The document at 'Path' is Length bytes; the most this connection takes is Limit. (Validation, 20452.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentTooLarge", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentTooLargeCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentTooLargeCode"/> class.</summary>
    public DocumentTooLargeCode()
        : base(20452, "DocumentTooLarge",
            ResultSeverities.ByName("Error"),
            "The document at '{Path}' is {Length} bytes; the most this connection takes is {Limit}.",
            isRetryable: false)
    {
    }
}
