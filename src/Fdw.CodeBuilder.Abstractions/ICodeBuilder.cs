using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Base interface for code builders that generate source code.
/// </summary>
public interface ICodeBuilder
{
    /// <summary>
    /// Builds the code and returns it as a string.
    /// </summary>
    /// <returns>The generated code.</returns>
    string Build();

    /// <summary>
    /// Gets the current indentation level.
    /// </summary>
    int IndentLevel { get; }

    /// <summary>
    /// Gets or sets the indentation string (default is 4 spaces).
    /// </summary>
    string IndentString { get; set; }
}
