using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set request states an abbreviation longer than the column that holds it (FDW-6129). It is refused before anything is written, so
/// the caller is told which value is wrong and how long it may be. Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
/// <remarks>The message is the caller's own input refused, so it is shown to them.</remarks>
[TypeOption(typeof(DataSetsResultCodes), "AbbreviationTooLong", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AbbreviationTooLongCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbbreviationTooLongCode"/> class.
    /// </summary>
    public AbbreviationTooLongCode()
        : base(20032, "AbbreviationTooLong",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: abbreviation has {length} characters and at most {maximum} are kept",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
