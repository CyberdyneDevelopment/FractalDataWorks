using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A record a stage rejected already carries a field named like one of the two columns a reject step fills in itself
/// (FDW-1142).
/// </summary>
/// <remarks>
/// Why 21002: the record is data that cannot be written as a rejected record without one of its own fields being replaced,
/// a validation failure that is not retried; it follows <c>RecordFailedValidation</c> (21001) in this collection's further
/// validation codes.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "RejectedRecordReservedColumn", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RejectedRecordReservedColumnCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RejectedRecordReservedColumnCode"/> class.
    /// </summary>
    public RejectedRecordReservedColumnCode()
        : base(21002, "RejectedRecordReservedColumn",
            ResultSeverities.ByName("Error"),
            "A record rejected by stage '{StageName}' of pipeline '{PipelineName}' already has a field named '{FieldName}', which the reject step fills in itself; rename or remove the field",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
