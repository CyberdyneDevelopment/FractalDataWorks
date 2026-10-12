using System;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// One step of a field mapping's transformer chain as the request states it, read by the stage mapper through
/// <see cref="IFieldTransformationReferenceSpec"/>.
/// </summary>
/// <param name="Transformation">The name of the Transformation, or <see langword="null"/> when it is identified by id.</param>
/// <param name="TransformationId">The id of the Transformation, or <see langword="null"/> when it is named.</param>
/// <param name="Ordinal">The position in the chain, counted from 1.</param>
public sealed record FieldTransformationReferenceSpec(string? Transformation, Guid? TransformationId, int Ordinal)
    : IFieldTransformationReferenceSpec;
