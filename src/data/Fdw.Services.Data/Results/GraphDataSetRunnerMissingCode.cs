using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The host that was asked to read a Graph data set registered no runner for its graph (FDW-5291): it is miswired, and an empty answer would hide that.
/// </summary>
/// <remarks>
/// The host that was asked to read a Graph data set registered no runner for its graph (FDW-5291): it is miswired, and an empty answer would hide that. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetRunnerMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetRunnerMissingCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetRunnerMissingCode"/> class.</summary>
    public GraphDataSetRunnerMissingCode()
        : base(21074, "GraphDataSetRunnerMissing",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' is a Graph data set and this host has nothing that runs a graph, so it cannot be read here",
            isRetryable: false)
    {
    }
}
