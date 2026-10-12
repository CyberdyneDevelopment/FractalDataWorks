using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A variation (a view) was refused because its place in a chain of views is wrong: its parent is not a view of the
/// workspace, the chain is deeper than allowed, it names a different data set than its parent's, or a stored filter on
/// the chain is not a filter tree (FDW-1032).
/// </summary>
/// <remarks>
/// A view of a view reads the root data set through its parent, so the chain has to be sound before anything is saved or
/// run. The reason names what is wrong. HTTP 400.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "DataSetVariationInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetVariationInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetVariationInvalidCode"/> class.</summary>
    public DataSetVariationInvalidCode()
        : base(20005, "DataSetVariationInvalid",
            ResultSeverities.ByName("Error"),
            "Variation '{name}' rejected: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
