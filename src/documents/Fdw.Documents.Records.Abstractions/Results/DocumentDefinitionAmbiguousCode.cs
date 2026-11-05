using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>The 'Kind' 'Code' is defined Count times at Level, so none is in force. (Conflict, 40453.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentDefinitionAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentDefinitionAmbiguousCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentDefinitionAmbiguousCode"/> class.</summary>
    public DocumentDefinitionAmbiguousCode()
        : base(40453, "DocumentDefinitionAmbiguous",
            ResultSeverities.ByName("Error"),
            "The '{Kind}' '{Code}' is defined {Count} times at the {Level} level for {Scope}, so none of them is in force.",
            isRetryable: false)
    {
    }
}
