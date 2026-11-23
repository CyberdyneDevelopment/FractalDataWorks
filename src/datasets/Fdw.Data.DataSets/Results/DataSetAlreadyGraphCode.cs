using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A conversion to a Graph data set named a data set that is already one (FDW-6972); converting twice cannot happen. Caller-input validation failure:
/// HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetAlreadyGraph", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetAlreadyGraphCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetAlreadyGraphCode"/> class.
    /// </summary>
    public DataSetAlreadyGraphCode()
        : base(20041, "DataSetAlreadyGraph",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' is already a Graph data set; there is nothing to convert",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
