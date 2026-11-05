using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>The public id 'Id' of a 'Entity' is not accepted: Reason. (Validation, 20455.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "PublicIdInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PublicIdInvalidCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PublicIdInvalidCode"/> class.</summary>
    public PublicIdInvalidCode()
        : base(20455, "PublicIdInvalid",
            ResultSeverities.ByName("Error"),
            "The public id '{Id}' of a '{Entity}' is not accepted: {Reason}.",
            isRetryable: false)
    {
    }
}
