using System.Text;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.CodeBuilder.CSharp;

/// <summary>
/// Base class for code builders.
/// </summary>
public abstract class CodeBuilderBase : ICodeBuilder
{
    /// <summary>
    /// The string builder for constructing the code.
    /// </summary>
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Gets the string builder for constructing the code.
    /// </summary>
    protected StringBuilder Builder => _builder;

    /// <summary>
    /// Gets or sets the indentation string.
    /// </summary>
    public string IndentString { get; set; } = "    ";

    /// <summary>
    /// Gets the current indentation level.
    /// </summary>
    public int IndentLevel { get; private set; }

    /// <summary>
    /// Builds the code and returns it as a string.
    /// </summary>
    /// <returns>The generated code.</returns>
    public abstract string Build();

    /// <summary>
    /// Appends a line with proper indentation.
    /// </summary>
    /// <param name="line">The line to append.</param>
    protected void AppendLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            _builder.AppendLine();
            return;
        }

        for (int i = 0; i < IndentLevel; i++)
        {
            _builder.Append(IndentString);
        }
        _builder.AppendLine(line);
    }

    /// <summary>
    /// Appends text without a newline.
    /// </summary>
    /// <param name="text">The text to append.</param>
    protected void Append(string text)
    {
        _builder.Append(text);
    }

    /// <summary>
    /// Increases the indentation level.
    /// </summary>
    protected void Indent()
    {
        IndentLevel++;
    }

    /// <summary>
    /// Decreases the indentation level.
    /// </summary>
    protected void Outdent()
    {
        if (IndentLevel > 0)
            IndentLevel--;
    }

    /// <summary>
    /// Clears the builder.
    /// </summary>
    protected void Clear()
    {
        _builder.Clear();
        IndentLevel = 0;
    }

    /// <summary>
    /// Escapes XML entities in text for safe use in XML documentation comments.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>The escaped text.</returns>
    protected static string EscapeXmlEntities(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }
}
