using System;
using System.Collections.Generic;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Represents a parsed syntax tree.
/// </summary>
public interface ISyntaxTree
{
    /// <summary>
    /// Gets the root node of the syntax tree.
    /// </summary>
    ISyntaxNode Root { get; }

    /// <summary>
    /// Gets the source text that was parsed.
    /// </summary>
    string SourceText { get; }

    /// <summary>
    /// Gets the language of the parsed code.
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Gets the file path, if available.
    /// </summary>
    string? FilePath { get; }

    /// <summary>
    /// Gets whether the tree contains any syntax errors.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Gets all error nodes in the tree.
    /// </summary>
    IEnumerable<ISyntaxNode> GetErrors();

    /// <summary>
    /// Finds all nodes of the specified type.
    /// </summary>
    /// <param name="nodeType">The type of nodes to find.</param>
    /// <returns>All matching nodes in the tree.</returns>
    IEnumerable<ISyntaxNode> FindNodes(string nodeType);

    /// <summary>
    /// Gets the node at the specified position.
    /// </summary>
    /// <param name="position">The character position in the source text.</param>
    /// <returns>The deepest node containing the position, or null if out of bounds.</returns>
    ISyntaxNode? GetNodeAtPosition(int position);

    /// <summary>
    /// Gets the node at the specified line and column.
    /// </summary>
    /// <param name="line">The line number (0-based).</param>
    /// <param name="column">The column number (0-based).</param>
    /// <returns>The deepest node containing the position, or null if out of bounds.</returns>
    ISyntaxNode? GetNodeAtLocation(int line, int column);
}
