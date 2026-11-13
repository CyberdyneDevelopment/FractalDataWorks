using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// An intended source states what the Graph data set already intends to read.
/// </summary>
/// <remarks>A data set states each intended source once (the row's unique index); added again it is refused by name, not left to the database. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetIntendedSourceDuplicate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetIntendedSourceDuplicateCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntendedSourceDuplicateCode"/> class.</summary>
    public GraphDataSetIntendedSourceDuplicateCode()
        : base(21431, "GraphDataSetIntendedSourceDuplicate",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' already intends to read {kind} '{name}'",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which data set already intends to read the data store or data set they named.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
