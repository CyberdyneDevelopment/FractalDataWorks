using System;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One component of a running flow as the live view reads it (FDW-3382). A value the run has not reported is <see langword="null"/>, never zero.
/// </summary>
/// <param name="Name">The component's name.</param>
/// <param name="Stage">The stage number the component runs in; 0 for a component that has none.</param>
/// <param name="StartedAt">When the component started, or <see langword="null"/> while it waits for its stage or a completion line.</param>
/// <param name="EndedAt">When the component ended, or <see langword="null"/> while it has not.</param>
/// <param name="Succeeded">Whether the component's run succeeded, or <see langword="null"/> until it ends.</param>
/// <param name="Rows">The rows the component has passed on so far (for a component with no outputs, the rows that reached it), or <see langword="null"/> until it starts.</param>
/// <param name="RowsIn">The rows that arrived on the component's inputs, whether or not it passes any on, or <see langword="null"/> until it starts.</param>
public sealed record FlowLiveComponentView(string Name, int Stage, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt, bool? Succeeded, long? Rows, long? RowsIn = null);
