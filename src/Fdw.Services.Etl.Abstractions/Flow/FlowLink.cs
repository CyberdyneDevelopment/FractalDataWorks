namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A link: the rows an output node of one component send to an input node of another (FDW-1386).
/// </summary>
/// <param name="FromComponent">The component the rows leave.</param>
/// <param name="FromOutput">The output node of that component.</param>
/// <param name="ToComponent">The component the rows arrive at.</param>
/// <param name="ToInput">The input node of that component.</param>
public sealed record FlowLink(string FromComponent, string FromOutput, string ToComponent, string ToInput);
