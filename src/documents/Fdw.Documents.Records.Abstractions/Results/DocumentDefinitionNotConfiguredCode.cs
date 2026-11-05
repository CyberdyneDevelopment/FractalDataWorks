using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>No level defines the 'Kind' 'Code' for 'Scope'. (Missing, 30452.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentDefinitionNotConfigured", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentDefinitionNotConfiguredCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentDefinitionNotConfiguredCode"/> class.</summary>
    public DocumentDefinitionNotConfiguredCode()
        : base(30452, "DocumentDefinitionNotConfigured",
            ResultSeverities.ByName("Error"),
            "No level defines the '{Kind}' '{Code}' for {Scope}: define it at the platform, the tenant or the organization.",
            isRetryable: false)
    {
    }
}
