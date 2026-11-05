using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>Organization 'OrganizationId' has no 'Entity' with the public id 'Id'. (Missing, 30451.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentRecordNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentRecordNotFoundCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRecordNotFoundCode"/> class.</summary>
    public DocumentRecordNotFoundCode()
        : base(30451, "DocumentRecordNotFound",
            ResultSeverities.ByName("Error"),
            "Organization '{OrganizationId}' has no '{Entity}' with the public id '{Id}'.",
            isRetryable: false)
    {
    }
}
