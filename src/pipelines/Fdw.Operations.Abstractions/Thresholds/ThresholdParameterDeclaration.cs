using System;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A parameter a threshold type takes (FDW-3935): how the measure it reports is taken, such as the field a quality rule reads or the limits of a range.</summary>
/// <param name="Name">The parameter name.</param>
/// <param name="ValueType">The type of its value.</param>
/// <param name="IsRequired">Whether a threshold of the type must state it.</param>
/// <param name="IsList">Whether it is stated as several values in order (the allowed values of a reference set) instead of one.</param>
/// <param name="Description">What it means, as the console shows it.</param>
public sealed record ThresholdParameterDeclaration(string Name, Type ValueType, bool IsRequired, bool IsList, string Description);
