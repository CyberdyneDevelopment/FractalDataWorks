using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One table a Built transformation writes: how many rows it has and, in the rows below it, the columns each row is made of.
/// Rows live in <c>transform.BuiltTransformationConfigurationTableOutput</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationTableOutputConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the output's name; whatever uses the transformation routes it by this name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets how the rows are decided: Each, Count or EachGroup.</summary>
    public string RowSource { get; set; } = string.Empty;

    /// <summary>Gets or sets the table the rows are made from (Each, EachGroup, and Count when repeated per row); null for a Count made once.</summary>
    public string? SourceTable { get; set; }

    /// <summary>Gets or sets the name a Count's position 1 to N is readable under; null otherwise.</summary>
    public string? IndexColumn { get; set; }

    /// <summary>Gets or sets the output's position among the table outputs.</summary>
    public int ExecutionOrder { get; set; }
}
