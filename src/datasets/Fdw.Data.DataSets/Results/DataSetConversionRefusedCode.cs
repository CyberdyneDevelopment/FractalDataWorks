using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set's conversion to a Graph data set is refused because parts of it have no place in a Graph data set (FDW-6972): each is named, none is
/// dropped, and nothing is changed. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetConversionRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetConversionRefusedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetConversionRefusedCode"/> class.
    /// </summary>
    public DataSetConversionRefusedCode()
        : base(20040, "DataSetConversionRefused",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' cannot be converted to a Graph data set: {parts} have no place in a Graph data set; nothing was changed",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
