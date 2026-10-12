using RecordParser.Builders.Writer;

namespace Fdw.Data.RowSources.FixedWidth.Abstractions;

/// <summary>
/// Defines one fixed-width field: its name, position, width, and padding rules.
/// Built at runtime from the container field schema — RecordParser's writer <c>Map</c> overload
/// takes the same (startIndex, length, padding, paddingChar) tuple.
/// </summary>
/// <remarks>
/// <see cref="Padding"/> is RecordParser's own <see cref="RecordParser.Builders.Writer.Padding"/>
/// enum — the format options are exactly the underlying library's options, not a wrapper.
/// </remarks>
public sealed class FixedWidthField
{
    /// <summary>
    /// Gets or sets the field/column name (the dictionary key on the produced row).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based start position of this field within the record line.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the fixed width (character count) of this field.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the padding alignment for this field (RecordParser's <see cref="Padding"/> enum).
    /// Default is <see cref="Padding.Right"/> (value left-aligned, padded on the right).
    /// </summary>
    public Padding Padding { get; set; } = Padding.Right;

    /// <summary>
    /// Gets or sets the character used to pad the field to its width. Default is space.
    /// </summary>
    public char PaddingChar { get; set; } = ' ';
}
