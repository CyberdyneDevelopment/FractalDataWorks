namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// An output node that is deliberately linked to nothing: its rows are discarded and counted (FDW-1386).
/// </summary>
/// <remarks>
/// Why stated: an output that goes nowhere by accident would silently lose rows. A graph is refused when an output is neither
/// linked nor listed here.
/// </remarks>
/// <param name="Component">The component.</param>
/// <param name="Output">The output node whose rows are discarded.</param>
public sealed record FlowDroppedOutput(string Component, string Output);
