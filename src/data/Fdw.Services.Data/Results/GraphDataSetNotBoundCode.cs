using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A command was addressed to an abstract Graph data set: it has fields and nothing feeds them yet.
/// </summary>
/// <remarks>
/// An abstract data set (FDW-5294) is saved with its fields out and, optionally, intended sources, before any container, mapping or filter
/// is chosen. It lists, shows in lineage and attaches to a workspace, but it has no rows to read until it is bound. Refused rather than
/// served, because an empty answer would read as a data set with no rows. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetNotBound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetNotBoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetNotBoundCode"/> class.</summary>
    public GraphDataSetNotBoundCode()
        : base(21032, "GraphDataSetNotBound",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' is an abstract Graph data set: nothing feeds its fields yet, so it is not bound and has no rows to read",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
