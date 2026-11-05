using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>Organization 'OrganizationId' has no 'TargetKind' 'TargetId' for a link to point at. (Missing, 30453.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentLinkTargetNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentLinkTargetNotFoundCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentLinkTargetNotFoundCode"/> class.</summary>
    public DocumentLinkTargetNotFoundCode()
        : base(30453, "DocumentLinkTargetNotFound",
            ResultSeverities.ByName("Error"),
            "Organization '{OrganizationId}' has no '{TargetKind}' '{TargetId}' for a link to point at.",
            isRetryable: false)
    {
    }
}
