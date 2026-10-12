namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A column a load definition names, and the field of the body where it is named.</summary>
/// <param name="Field">The body field that names it, for a problem to point at.</param>
/// <param name="Column">The column's name.</param>
public sealed record NamedColumn(string Field, string Column);
