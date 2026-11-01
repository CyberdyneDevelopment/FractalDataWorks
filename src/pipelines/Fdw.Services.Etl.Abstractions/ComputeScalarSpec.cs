namespace Fdw.Services.Etl.Abstractions;

/// <summary>One single value a Compute stage hands to an input of the Built transformation it applies (FDW-1231).</summary>
/// <param name="InputName">The input of the transformation that receives the value.</param>
/// <param name="ValueText">The value, written as text.</param>
/// <param name="ValueType">Number, Flag or Text.</param>
public sealed record ComputeScalarSpec(string InputName, string ValueText, string ValueType);
