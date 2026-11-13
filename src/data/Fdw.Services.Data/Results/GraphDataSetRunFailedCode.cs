using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The graph of a Graph data set failed while it ran (FDW-5291); the reason is the engine's own.
/// </summary>
/// <remarks>
/// The graph of a Graph data set failed while it ran (FDW-5291); the reason is the engine's own. No rows are returned. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetRunFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetRunFailedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetRunFailedCode"/> class.</summary>
    public GraphDataSetRunFailedCode()
        : base(21073, "GraphDataSetRunFailed",
            ResultSeverities.ByName("Error"),
            "The graph of data set '{dataSet}' could not finish: {reason}",
            isRetryable: false)
    {
    }
}
