namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A completion line: the component at its end does not start until the component at its start has finished (FDW-3640).
/// </summary>
/// <param name="FromComponent">The component that must have finished.</param>
/// <param name="ToComponent">The component that waits for it.</param>
public sealed record FlowCompletion(string FromComponent, string ToComponent);
