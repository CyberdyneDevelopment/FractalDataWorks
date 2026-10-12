using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One group-by field of an Aggregate transformation.
/// Rows live in <c>transform.AggregateTransformationConfigurationGroupByField</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AggregateTransformationGroupByFieldConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and position.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the input field grouped by.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's position in the grouping.</summary>
    public int Ordinal { get; set; }
}
