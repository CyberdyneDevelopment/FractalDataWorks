namespace Fdw.Commands.Data.ValueSources;

/// <summary>A column value that is the same for every row: a literal the caller supplies, sent as a parameter.</summary>
public sealed class ConstantValueSource : ColumnValueSource
{
    /// <summary>The kind name connection types render.</summary>
    public const string KindName = "Constant";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantValueSource"/> class.
    /// </summary>
    /// <param name="value">The constant, or <see langword="null"/> for a database NULL.</param>
    public ConstantValueSource(object? value)
        : base(KindName)
    {
        Value = value;
    }

    /// <summary>Gets the constant value; <see langword="null"/> means a database NULL.</summary>
    public object? Value { get; }
}
