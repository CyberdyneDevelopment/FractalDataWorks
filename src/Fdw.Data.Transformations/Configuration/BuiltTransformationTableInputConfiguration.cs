using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One table a Built transformation reads, by name: the shape it expects, never where the rows come from.
/// Rows live in <c>transform.BuiltTransformationConfigurationTableInput</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationTableInputConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the table's name inside the transformation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the table's position among the transformation's table inputs.</summary>
    public int ExecutionOrder { get; set; }
}
