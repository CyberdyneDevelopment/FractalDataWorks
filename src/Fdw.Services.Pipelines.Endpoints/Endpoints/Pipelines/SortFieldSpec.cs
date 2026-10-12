using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A sort field of a pipeline transform request, read through the engine's <see cref="ISortFieldSpec"/>.</summary>
/// <param name="FieldName">The field the transform orders by.</param>
/// <param name="Direction">The direction name, Ascending or Descending.</param>
public sealed record SortFieldSpec(string FieldName, string Direction) : ISortFieldSpec;
