namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// What a request states for a Union stage beyond the members every stage has: the data set whose rows it appends.
/// </summary>
/// <remarks>
/// FDW-1120 phase 4. The stage's field mappings (<see cref="ITransformOperationSpec.FieldMappings"/>) say how the fields of
/// that data set become the fields the stage's output has; the union source says which data set it is.
/// </remarks>
public interface ITransformUnionSpec
{
    /// <summary>Gets the name of the data set a Union stage appends after the last row of the pipeline's source; null when the request states none.</summary>
    string? UnionSource { get; }
}
