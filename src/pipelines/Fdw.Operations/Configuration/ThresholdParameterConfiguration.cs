using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>A parameter of a threshold (FDW-3935): how the measure its type reports is taken. Child of ThresholdImplementationConfiguration.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Operations")]
public sealed partial class ThresholdParameterConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parameter name; one the threshold type declares.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text in the invariant culture.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets how Value is read: Int64, Double, String, Boolean, TimeSpan or DateTimeOffset.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the place among the values of a list parameter, counted from 1.</summary>
    public int Ordinal { get; set; }
}
