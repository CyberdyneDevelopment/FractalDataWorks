using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A stage's condition text cannot be read as a condition (FDW-1120).
/// </summary>
/// <remarks>
/// Why 21000: its meaning is InvalidInput (the 20001 family of the result code catalog), and 20001 is already
/// <c>InvalidPipelineStep</c> in this collection, whose ids are unique; the 21xxx numbers are this collection's further
/// validation codes.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "RecordPredicateInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RecordPredicateInvalidCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordPredicateInvalidCode"/> class.
    /// </summary>
    public RecordPredicateInvalidCode()
        : base(21000, "RecordPredicateInvalid",
            ResultSeverities.ByName("Error"),
            "The condition '{Expression}' cannot be read: {Reason}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
