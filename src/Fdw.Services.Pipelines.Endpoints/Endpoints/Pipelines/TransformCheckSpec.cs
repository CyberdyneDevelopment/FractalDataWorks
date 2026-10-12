using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A check of a Validate stage as the stage mapping reads it.</summary>
/// <param name="Name">The check's name.</param>
/// <param name="Expression">The condition a record must meet.</param>
public sealed record TransformCheckSpec(string Name, string Expression) : ITransformCheckSpec;
