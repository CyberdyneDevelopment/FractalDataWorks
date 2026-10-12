namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A column that holds the same stated value in every row.</summary>
public sealed class LiteralColumn : OutputColumnDefinition
{
    private readonly object? _value;

    /// <summary>Initializes a new instance of the <see cref="LiteralColumn"/> class.</summary>
    /// <param name="name">The column's name in the output.</param>
    /// <param name="value">The value every row holds.</param>
    public LiteralColumn(string name, object? value)
        : base(name)
    {
        _value = value;
    }

    /// <inheritdoc />
    public override string Kind => "Literal";

    /// <inheritdoc />
    public override object? Literal => _value;

    /// <inheritdoc />
    public override object? Value(OutputRowContext row) => _value;
}
