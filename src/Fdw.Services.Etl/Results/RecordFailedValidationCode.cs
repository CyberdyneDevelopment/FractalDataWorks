using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A record failed a check of a Validate stage whose failing action stops the run (FDW-1120).
/// </summary>
/// <remarks>
/// Why 21001: the record is data that does not meet the condition the pipeline states, a validation failure that is not
/// retried; it follows <c>RecordPredicateInvalid</c> (21000) in this collection's further validation codes.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "RecordFailedValidation", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RecordFailedValidationCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordFailedValidationCode"/> class.
    /// </summary>
    public RecordFailedValidationCode()
        : base(21001, "RecordFailedValidation",
            ResultSeverities.ByName("Error"),
            "A record failed check '{CheckName}' of stage '{StageName}'",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
