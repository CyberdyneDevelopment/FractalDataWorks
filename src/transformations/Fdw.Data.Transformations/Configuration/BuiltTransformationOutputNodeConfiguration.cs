using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One piece of the formula of an output of a Built transformation.
/// Rows live in <c>transform.BuiltTransformationConfigurationOutputNode</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationOutputNodeConfiguration : IGenericConfiguration, IBuiltFormulaNodeRow
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner, output and node number.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the output whose formula this piece belongs to.</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number that identifies this piece within its output.</summary>
    public int NodeNumber { get; set; }

    /// <summary>Gets or sets the number of the parent piece; null for the root of the formula.</summary>
    public int? ParentNodeNumber { get; set; }

    /// <summary>Gets or sets the piece's place among its parent's operands.</summary>
    public int Position { get; set; }

    /// <summary>Gets or sets the operator, the name of a calculation node type.</summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>Gets or sets the field or result name of a Field or Ref piece.</summary>
    public string? NodeName { get; set; }

    /// <summary>Gets or sets the numeric literal of a Constant piece.</summary>
    public decimal? NumberValue { get; set; }

    /// <summary>Gets or sets the boolean literal of a Constant piece.</summary>
    public bool? FlagValue { get; set; }

    /// <summary>Gets or sets the decimal places of a Round piece.</summary>
    public int? Places { get; set; }

    /// <summary>Gets or sets the midpoint rounding mode of a Round piece.</summary>
    public string? Mode { get; set; }
}
