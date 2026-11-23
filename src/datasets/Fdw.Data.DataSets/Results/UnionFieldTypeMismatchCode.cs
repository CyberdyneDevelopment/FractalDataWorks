using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A union stacks every source's rows, so a field has one data type in every source (FDW-6946). A source that gives a field another type is refused
/// when the data set is read, never converted by a guess. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "UnionFieldTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnionFieldTypeMismatchCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnionFieldTypeMismatchCode"/> class.
    /// </summary>
    public UnionFieldTypeMismatchCode()
        : base(20037, "UnionFieldTypeMismatch",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' is a union, and its field '{field}' is {type} from source '{firstSource}' but {otherType} from source '{source}'; every source of a union gives a field the same type",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
