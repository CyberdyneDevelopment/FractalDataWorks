using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The connection 'Connection' does not create temporary read links. (Validation, 20454.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentReadLinksNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentReadLinksNotSupportedCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentReadLinksNotSupportedCode"/> class.</summary>
    public DocumentReadLinksNotSupportedCode()
        : base(20454, "DocumentReadLinksNotSupported",
            ResultSeverities.ByName("Error"),
            "The connection '{Connection}' does not create temporary read links.",
            isRetryable: false)
    {
    }
}
