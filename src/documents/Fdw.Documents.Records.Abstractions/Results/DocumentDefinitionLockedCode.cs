using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>The 'Kind' 'Code' cannot be defined at Level: Reason. (Conflict, 40452.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentDefinitionLocked", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentDefinitionLockedCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentDefinitionLockedCode"/> class.</summary>
    public DocumentDefinitionLockedCode()
        : base(40452, "DocumentDefinitionLocked",
            ResultSeverities.ByName("Error"),
            "The '{Kind}' '{Code}' cannot be defined at the {Level} level: {Reason}.",
            isRetryable: false)
    {
    }
}
