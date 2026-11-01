using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One input of a Built transformation: a field it reads.
/// Rows live in <c>transform.BuiltTransformationConfigurationInput</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationInputConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value type: Number or Flag.</summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>Gets or sets the input's position among the transformation's inputs.</summary>
    public int ExecutionOrder { get; set; }
}
