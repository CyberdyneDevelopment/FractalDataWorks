using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A preview of a Graph data set draft named a component and output to read at that the draft does not have, or named one without the other
/// </summary>
/// <remarks>
/// A preview of a Graph data set draft named a component and output to read at that the draft does not have, or named one without the other (FDW-5454). Nothing is read. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetPreviewEndNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetPreviewEndNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetPreviewEndNotFoundCode"/> class.</summary>
    public GraphDataSetPreviewEndNotFoundCode()
        : base(21413, "GraphDataSetPreviewEndNotFound",
            ResultSeverities.ByName("Error"),
            "The preview of Graph data set draft '{dataSet}' cannot be read at component '{component}', output '{output}': {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which component and output they asked to read at and why it cannot be read there.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
