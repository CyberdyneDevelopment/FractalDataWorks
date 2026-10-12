using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A data view was refused because its chart cannot be drawn: the chart type is not one the platform has,
/// the bindings do not fit it, a bound field is not a field of the data set (or is of the wrong kind), or the
/// stored filter does not resolve against the data set (FDW-1023).
/// </summary>
/// <remarks>
/// Refused rather than saved and found out when the chart is opened: the view is created once and read by
/// whoever it is shared with, so a chart that fails to draw is a failure for them, not for the author. The
/// reason names the slot or the field. HTTP 400.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "DataViewSpecInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataViewSpecInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataViewSpecInvalidCode"/> class.</summary>
    public DataViewSpecInvalidCode()
        : base(20003, "DataViewSpecInvalid",
            ResultSeverities.ByName("Error"),
            "Data view '{name}' rejected: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
