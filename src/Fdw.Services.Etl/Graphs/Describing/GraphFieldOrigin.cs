namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// Where a field at a node came from (FDW-2156): the field of a data set that a source read, followed through the components that pass it on.
/// </summary>
/// <remarks>
/// A field with no origin was made by the pipeline (a calculation, a constant, an aggregate) or has one this describe cannot follow; it is never given
/// the origin of a field it merely resembles.
/// </remarks>
/// <param name="DataSet">The data set the field was read from.</param>
/// <param name="Field">The field of that data set.</param>
public sealed record GraphFieldOrigin(string DataSet, string Field);
