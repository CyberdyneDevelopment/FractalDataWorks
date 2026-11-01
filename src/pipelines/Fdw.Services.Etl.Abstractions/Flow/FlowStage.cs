namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The stage a component runs in (FDW-2040): a component with a stage waits until every component of a lower stage, and every component with no stage, has finished.
/// A component with no stage is not listed and waits for nothing.
/// </summary>
/// <param name="Component">The component's name.</param>
/// <param name="Stage">The stage number, at least 1.</param>
public sealed record FlowStage(string Component, int Stage);
