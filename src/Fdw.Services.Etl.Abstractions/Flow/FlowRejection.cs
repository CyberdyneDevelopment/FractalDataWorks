using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What one component sent out of one error output in a run: how many rows and the reasons it gave, never the rows (FDW-1720).
/// </summary>
/// <param name="Component">The component that rejected the rows.</param>
/// <param name="Output">The error output node they left through.</param>
/// <param name="Count">How many rows left through the output.</param>
/// <param name="Reasons">The distinct reasons the rows carried with how many rows each had, the commonest first. Rows with no stated reason are in <paramref name="Count"/> and in no reason; reasons past the most the record keeps are in <paramref name="Count"/> only.</param>
public sealed record FlowRejection(string Component, string Output, long Count, IReadOnlyList<FlowRejectionReason> Reasons);
