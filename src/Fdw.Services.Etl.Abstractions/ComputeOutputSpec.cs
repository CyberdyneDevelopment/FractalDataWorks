namespace Fdw.Services.Etl.Abstractions;

/// <summary>Where one table output of the Built transformation a Compute stage applies goes (FDW-1231).</summary>
/// <param name="OutputName">The table output of the transformation this routes.</param>
/// <param name="Route">Replace (the rows become the stage's rows) or Named (kept under <paramref name="RouteName"/> for later stages).</param>
/// <param name="RouteName">The name a Named route keeps the rows under; null for Replace.</param>
public sealed record ComputeOutputSpec(string OutputName, string Route, string? RouteName);
