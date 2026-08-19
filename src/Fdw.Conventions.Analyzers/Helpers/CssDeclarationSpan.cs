namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// One <c>property:value</c> declaration located inside a markup style attribute, with the document
/// span it occupies so a diagnostic can point at the declaration rather than at the attribute.
/// </summary>
internal readonly struct CssDeclarationSpan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssDeclarationSpan"/> struct.
    /// </summary>
    /// <param name="start">Document offset of the first character of the declaration.</param>
    /// <param name="length">Length of the declaration text.</param>
    /// <param name="property">The declaration's property name, lower-cased and trimmed.</param>
    /// <param name="value">The declaration's value, trimmed.</param>
    internal CssDeclarationSpan(int start, int length, string property, string value)
    {
        this.Start = start;
        this.Length = length;
        this.Property = property;
        this.Value = value;
    }

    /// <summary>
    /// Gets the document offset of the first character of the declaration.
    /// </summary>
    internal int Start { get; }

    /// <summary>
    /// Gets the length of the declaration text.
    /// </summary>
    internal int Length { get; }

    /// <summary>
    /// Gets the declaration's property name, lower-cased and trimmed.
    /// </summary>
    internal string Property { get; }

    /// <summary>
    /// Gets the declaration's value, trimmed.
    /// </summary>
    internal string Value { get; }

    /// <summary>
    /// Gets the declaration as it reads in the message: <c>property:value</c>.
    /// </summary>
    internal string Text => this.Property + ":" + this.Value;
}
