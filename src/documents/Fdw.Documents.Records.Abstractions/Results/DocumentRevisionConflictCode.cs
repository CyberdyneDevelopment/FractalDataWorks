using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>The document records of organization 'OrganizationId' are at revision 'Current', not 'Expected'. (Conflict, 40451.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentRevisionConflict", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentRevisionConflictCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRevisionConflictCode"/> class.</summary>
    public DocumentRevisionConflictCode()
        : base(40451, "DocumentRevisionConflict",
            ResultSeverities.ByName("Error"),
            "The document records of organization '{OrganizationId}' are at revision {Current}, not {Expected}: read them again and retry.",
            isRetryable: false)
    {
    }
}
