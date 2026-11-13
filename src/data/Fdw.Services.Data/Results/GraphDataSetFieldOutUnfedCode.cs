using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A field out of a bound Graph data set does not state the component output that feeds it.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>fieldsOut/{field}</c>. A data set with no graph yet (abstract) feeds none of its fields and is not reported. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldOutUnfed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldOutUnfedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldOutUnfedCode"/> class.</summary>
    public GraphDataSetFieldOutUnfedCode()
        : base(21400, "GraphDataSetFieldOutUnfed",
            ResultSeverities.ByName("Error"),
            "Field out '{field}' of Graph data set '{dataSet}' is not fed by a component output (it names no component and output field): its graph is bound, so every field out must come from a component",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
