using System;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>What one component of a data flow did in a run (FDW-1602): when it started and ended, and whether it succeeded.</summary>
/// <param name="Name">The component's name.</param>
/// <param name="StartedAt">When the component's run began; a component that waited for another began when the wait ended.</param>
/// <param name="EndedAt">When the component's run ended, outputs closed and unread input released.</param>
/// <param name="Succeeded">Whether the component ended without failing; a component stopped because another failed did not succeed.</param>
/// <param name="Stage">The component's stage number (FDW-2060); 0 for a component that has none, which runs first.</param>
public sealed record FlowComponentReport(string Name, DateTimeOffset StartedAt, DateTimeOffset EndedAt, bool Succeeded, int Stage = 0);
