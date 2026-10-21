using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The storage refused the request for 'Path': Reason. (Dependency, 70450.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentStorageRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentStorageRefusedCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentStorageRefusedCode"/> class.</summary>
    public DocumentStorageRefusedCode()
        : base(70450, "DocumentStorageRefused",
            ResultSeverities.ByName("Error"),
            "The storage refused the request for '{Path}': {Reason}.",
            isRetryable: true)
    {
    }
}
