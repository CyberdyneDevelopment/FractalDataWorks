namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A column that carries a visible value through unchanged, whatever its type.</summary>
public sealed class CopyColumn : OutputColumnDefinition
{
    private readonly string _source;

    /// <summary>Initializes a new instance of the <see cref="CopyColumn"/> class.</summary>
    /// <param name="name">The column's name in the output.</param>
    /// <param name="source">The name of the visible value that is copied.</param>
    public CopyColumn(string name, string source)
        : base(name)
    {
        _source = source;
    }

    /// <inheritdoc />
    public override string Kind => "Copy";

    /// <inheritdoc />
    public override string? Source => _source;

    /// <inheritdoc />
    public override string? Reads => _source;

    /// <inheritdoc />
    public override object? Value(OutputRowContext row) => row.Read(_source);
}
