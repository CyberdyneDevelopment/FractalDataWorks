namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>A join, union or lookup the planner left in memory, and why (FDW-6969).</summary>
/// <param name="Component">The component's name.</param>
/// <param name="Reason">Why it runs in memory.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushdownLeftInMemory(string Component, string Reason);
