using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One column of a table output: copied from a visible value, a stated value, or a formula whose pieces are node rows.
/// Rows live in <c>transform.BuiltTransformationConfigurationTableOutputColumn</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationTableOutputColumnConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the table output this column belongs to.</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column name in the output.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: Copy, Literal or Formula.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the visible value a Copy column carries through; null otherwise.</summary>
    public string? SourceName { get; set; }

    /// <summary>Gets or sets the stated value of a Literal column, written as text; null otherwise.</summary>
    public string? LiteralText { get; set; }

    /// <summary>Gets or sets the type of the stated value: Text, Number or Flag; null otherwise.</summary>
    public string? LiteralType { get; set; }

    /// <summary>Gets or sets the column's position within its output.</summary>
    public int ExecutionOrder { get; set; }
}
