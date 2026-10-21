using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The content type 'ContentType' is not allowed for 'Path'. (Validation, 20453.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentContentTypeNotAllowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentContentTypeNotAllowedCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentContentTypeNotAllowedCode"/> class.</summary>
    public DocumentContentTypeNotAllowedCode()
        : base(20453, "DocumentContentTypeNotAllowed",
            ResultSeverities.ByName("Error"),
            "The content type '{ContentType}' is not allowed for '{Path}'.",
            isRetryable: false)
    {
    }
}
