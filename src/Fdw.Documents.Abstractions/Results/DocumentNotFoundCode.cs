using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>There is no document at 'Path'. (NotFound, 30450.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentNotFoundCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentNotFoundCode"/> class.</summary>
    public DocumentNotFoundCode()
        : base(30450, "DocumentNotFound",
            ResultSeverities.ByName("Error"),
            "There is no document at '{Path}'.",
            isRetryable: false)
    {
    }
}
