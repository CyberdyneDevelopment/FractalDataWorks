using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Map implementation of a Transformation: renames, retypes and defaults the fields of its one input, one field mapping at a time.
/// Ports: input -> output. Rows live in <c>transform.MapTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class MapTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>The implementation name a Map transformation's domain row carries.</summary>
    public const string ImplementationName = "Map";

    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row; the table has no Id of its own.</remarks>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsSystemProvided { get; set; }


    /// <summary>Gets or sets the field mappings, applied in Ordinal order.</summary>
    /// <remarks>One row each in <c>transform.MapTransformationConfigurationFieldMapping</c>.</remarks>
    [NotMapped]
    public IList<MapTransformationFieldMappingConfiguration> FieldMappings { get; set; } = new List<MapTransformationFieldMappingConfiguration>();
}
