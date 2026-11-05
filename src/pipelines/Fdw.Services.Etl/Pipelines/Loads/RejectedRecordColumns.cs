using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// The two columns a reject step's destination carries besides the rejected record's own fields (FDW-1142): why the
/// record was rejected and which component rejected it. They are matched to the destination's columns by name, so the names are
/// fixed. The names are stated once, on the Reject load phase (which lists them as its reserved columns); this class gives the engine's code the short names.
/// </summary>
public static class RejectedRecordColumns
{
    /// <summary>The column that receives the reason a component rejected the record.</summary>
    public const string Reason = RejectLoadPhase.ReasonColumn;

    /// <summary>The column that receives the name of the component that rejected the record.</summary>
    public const string Component = RejectLoadPhase.ComponentColumn;

    /// <summary>Gets the two reserved column names.</summary>
    public static IReadOnlyList<string> All { get; } = [Reason, Component];
}
