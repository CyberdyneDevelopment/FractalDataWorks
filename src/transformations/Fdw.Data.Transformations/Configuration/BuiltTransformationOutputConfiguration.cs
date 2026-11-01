using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One output of a Built transformation: a result, kept or used only by later outputs.
/// Rows live in <c>transform.BuiltTransformationConfigurationOutput</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationOutputConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the result name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value saying whether the result is written out; false for a working number.</summary>
    public bool IsKept { get; set; }

    /// <summary>Gets or sets the output's position; a later output may refer to an earlier one.</summary>
    public int ExecutionOrder { get; set; }
}
