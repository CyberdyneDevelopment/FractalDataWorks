using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The path 'Path' is not a document path: Reason. (Validation, 20451.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentPathInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentPathInvalidCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentPathInvalidCode"/> class.</summary>
    public DocumentPathInvalidCode()
        : base(20451, "DocumentPathInvalid",
            ResultSeverities.ByName("Error"),
            "The path '{Path}' is not a document path: {Reason}.",
            isRetryable: false)
    {
    }
}
