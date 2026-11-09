using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// A value written to a date column is not a date.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidDateValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidDateValueCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDateValueCode"/> class.
    /// </summary>
    public InvalidDateValueCode()
        : base(21030, "InvalidDateValue",
            ResultSeverities.ByName("Error"),
            "Column '{Column}' of '{Container}' takes a date; a value of kind {ValueKind} is not a date",
            isRetryable: false)
    {
    }
}
