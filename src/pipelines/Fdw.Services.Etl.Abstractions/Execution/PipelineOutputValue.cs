namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// One output a run gave back (FDW-6136): its name, its stated data type and its value as invariant text.
/// </summary>
/// <param name="Name">The output's name, as its pipeline declares it.</param>
/// <param name="DataType">The <c>DataTypes</c> option name the pipeline states for it.</param>
/// <param name="Text">The value as invariant text; <see langword="null"/> when the variable held no value.</param>
public sealed record PipelineOutputValue(string Name, string DataType, string? Text);
