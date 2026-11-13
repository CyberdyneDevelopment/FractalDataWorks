using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A route named a field out that the Graph data set does not have.
/// </summary>
/// <remarks>Notes are added to, answered on and removed from a field out by the field's name (FDW-5452). HTTP 404.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldNotFoundCode"/> class.</summary>
    public GraphDataSetFieldNotFoundCode()
        : base(31430, "GraphDataSetFieldNotFound",
            ResultSeverities.ByName("Error"),
            "Field '{field}' is not a field out of Graph data set '{dataSet}'",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which field the Graph data set does not have.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
