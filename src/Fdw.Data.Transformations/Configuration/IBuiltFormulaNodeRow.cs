namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One piece of a stored formula: the members every kind of node row shares, so one reader and one writer turn any of them into
/// and out of the calculation engine's tree.
/// </summary>
public interface IBuiltFormulaNodeRow
{
    /// <summary>Gets or sets the number that identifies the piece within its formula.</summary>
    int NodeNumber { get; set; }

    /// <summary>Gets or sets the number of the piece this one is an operand of, or null for the root.</summary>
    int? ParentNodeNumber { get; set; }

    /// <summary>Gets or sets the place among the parent's operands.</summary>
    int Position { get; set; }

    /// <summary>Gets or sets the operator: the name of a calculation node type.</summary>
    string Operator { get; set; }

    /// <summary>Gets or sets the name a Field reads or a Ref refers to.</summary>
    string? NodeName { get; set; }

    /// <summary>Gets or sets the number a Constant states.</summary>
    decimal? NumberValue { get; set; }

    /// <summary>Gets or sets the true or false a Constant states.</summary>
    bool? FlagValue { get; set; }

    /// <summary>Gets or sets the decimal places a Round keeps.</summary>
    int? Places { get; set; }

    /// <summary>Gets or sets the midpoint rounding mode of a Round.</summary>
    string? Mode { get; set; }
}
