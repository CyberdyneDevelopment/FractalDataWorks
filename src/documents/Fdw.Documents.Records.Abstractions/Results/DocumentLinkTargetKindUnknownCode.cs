using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>A link cannot point at a 'TargetKind': the target kinds are Kinds. (Validation, 20456.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentLinkTargetKindUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentLinkTargetKindUnknownCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentLinkTargetKindUnknownCode"/> class.</summary>
    public DocumentLinkTargetKindUnknownCode()
        : base(20456, "DocumentLinkTargetKindUnknown",
            ResultSeverities.ByName("Error"),
            "A link cannot point at a '{TargetKind}': the target kinds are {Kinds}.",
            isRetryable: false)
    {
    }
}
