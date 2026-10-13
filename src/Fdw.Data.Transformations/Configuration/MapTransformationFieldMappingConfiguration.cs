using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One field mapping of a Map transformation: <see cref="SourceField"/> of the input becomes <see cref="DestinationField"/> of the output.
/// Rows live in <c>transform.MapTransformationConfigurationFieldMapping</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class MapTransformationFieldMappingConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and position.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the input field read.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the output field written.</summary>
    public string DestinationField { get; set; } = string.Empty;

    /// <summary>Gets or sets the value written when the source is empty.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets the type the value is converted to.</summary>
    public string? TargetType { get; set; }

    /// <summary>Gets or sets whether an empty source fails the record.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the mapping runs.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the mapping's position.</summary>
    public int Ordinal { get; set; }
}
