namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// One mapping of a Compute stage as a request states it: which field of the record feeds one input of the Built transformation.
/// </summary>
/// <param name="InputName">The name of the input the Built transformation declares.</param>
/// <param name="SourceField">The field of the record that feeds the input.</param>
public sealed record ComputeInputSpec(string InputName, string SourceField);
