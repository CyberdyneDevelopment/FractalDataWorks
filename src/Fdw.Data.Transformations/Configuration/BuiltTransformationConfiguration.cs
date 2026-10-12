using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Built implementation of a Transformation: a named calculation with declared inputs, ordered outputs and, for each
/// output, a formula stored as node rows. The calculation engine runs it. Rows live in
/// <c>transform.BuiltTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>The implementation name a Built transformation's domain row carries.</summary>
    public const string ImplementationName = "Built";

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

    /// <summary>Gets or sets the fields the calculation reads, in ExecutionOrder.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationInput</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationInputConfiguration> Inputs { get; set; } = new List<BuiltTransformationInputConfiguration>();

    /// <summary>Gets or sets the results the calculation produces, in ExecutionOrder.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationOutput</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationOutputConfiguration> Outputs { get; set; } = new List<BuiltTransformationOutputConfiguration>();

    /// <summary>Gets or sets the tables the transformation reads, by shape, in ExecutionOrder.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationTableInput</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationTableInputConfiguration> TableInputs { get; set; } = new List<BuiltTransformationTableInputConfiguration>();

    /// <summary>Gets or sets the columns of every table input.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationTableInputColumn</c>; a row belongs to a table by TableName.</remarks>
    [NotMapped]
    public IList<BuiltTransformationTableInputColumnConfiguration> TableInputColumns { get; set; } = new List<BuiltTransformationTableInputColumnConfiguration>();

    /// <summary>Gets or sets the totals and lookups formulas can read, in ExecutionOrder.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationDerived</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationDerivedConfiguration> DerivedValues { get; set; } = new List<BuiltTransformationDerivedConfiguration>();

    /// <summary>Gets or sets the tables the transformation writes, in ExecutionOrder.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationTableOutput</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationTableOutputConfiguration> TableOutputs { get; set; } = new List<BuiltTransformationTableOutputConfiguration>();

    /// <summary>Gets or sets the columns of every table output.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationTableOutputColumn</c>; a row belongs to an output by OutputName.</remarks>
    [NotMapped]
    public IList<BuiltTransformationTableOutputColumnConfiguration> TableOutputColumns { get; set; } = new List<BuiltTransformationTableOutputColumnConfiguration>();

    /// <summary>Gets or sets the columns that define the groups of the outputs made of one row per group.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationTableOutputGroupColumn</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationTableOutputGroupColumnConfiguration> TableOutputGroupColumns { get; set; } = new List<BuiltTransformationTableOutputGroupColumnConfiguration>();

    /// <summary>Gets or sets the pieces of every table output formula: column formulas, row conditions and row counts.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationTableOutputNode</c>.</remarks>
    [NotMapped]
    public IList<BuiltTransformationTableOutputNodeConfiguration> TableOutputNodes { get; set; } = new List<BuiltTransformationTableOutputNodeConfiguration>();

    /// <summary>Gets or sets the pieces of every output's formula.</summary>
    /// <remarks>One row each in <c>transform.BuiltTransformationConfigurationOutputNode</c>; a row belongs to an output by OutputName.</remarks>
    [NotMapped]
    public IList<BuiltTransformationOutputNodeConfiguration> Nodes { get; set; } = new List<BuiltTransformationOutputNodeConfiguration>();
}
