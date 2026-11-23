using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A union stacks every source's rows in the data set's field order, so every source maps every field (FDW-6946). A field a source does not map is
/// refused when the data set is saved and before it is read, never read as null. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "UnionFieldNotMapped", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnionFieldNotMappedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnionFieldNotMappedCode"/> class.
    /// </summary>
    public UnionFieldNotMappedCode()
        : base(20036, "UnionFieldNotMapped",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' is a union, and its source '{source}' does not map its field '{field}'; every source of a union maps every field",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
