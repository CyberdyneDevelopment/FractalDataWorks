using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>A document already exists at 'Path' and the write is create-only. (Conflict, 40450.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentAlreadyExists", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentAlreadyExistsCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentAlreadyExistsCode"/> class.</summary>
    public DocumentAlreadyExistsCode()
        : base(40450, "DocumentAlreadyExists",
            ResultSeverities.ByName("Error"),
            "A document already exists at '{Path}' and the write is create-only.",
            isRetryable: false)
    {
    }
}
