namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A signal line seen from the component it feeds (FDW-6134): the decision it leaves and the output of that decision.</summary>
/// <param name="Component">The decision the line leaves.</param>
/// <param name="Output">The output of that decision.</param>
public sealed record FlowSignal(string Component, string Output);
