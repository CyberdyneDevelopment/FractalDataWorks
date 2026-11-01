using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One piece of a formula of a table output: the formula of a column, or its row condition, or its row count.
/// Rows live in <c>transform.BuiltTransformationConfigurationTableOutputNode</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationTableOutputNodeConfiguration : IGenericConfiguration, IBuiltFormulaNodeRow
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the table output this piece belongs to.</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column whose formula this piece belongs to; null for a condition or a count.</summary>
    public string? ColumnName { get; set; }

    /// <summary>Gets or sets what the formula is: Column, Where or Count.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Gets or sets the number that identifies the piece within its formula.</summary>
    public int NodeNumber { get; set; }

    /// <summary>Gets or sets the number of the piece this one is an operand of, or null for the root.</summary>
    public int? ParentNodeNumber { get; set; }

    /// <summary>Gets or sets the place among the parent's operands.</summary>
    public int Position { get; set; }

    /// <summary>Gets or sets the operator: the name of a calculation node type.</summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>Gets or sets the name a Field reads or a Ref refers to.</summary>
    public string? NodeName { get; set; }

    /// <summary>Gets or sets the number a Constant states.</summary>
    public decimal? NumberValue { get; set; }

    /// <summary>Gets or sets the true or false a Constant states.</summary>
    public bool? FlagValue { get; set; }

    /// <summary>Gets or sets the decimal places a Round keeps.</summary>
    public int? Places { get; set; }

    /// <summary>Gets or sets the midpoint rounding mode of a Round.</summary>
    public string? Mode { get; set; }
}
