using System;
using System.Collections.Generic;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Represents a node in the syntax tree.
/// </summary>
public interface ISyntaxNode
{
    /// <summary>
    /// Gets the type of this syntax node.
    /// </summary>
    string NodeType { get; }

    /// <summary>
    /// Gets the text content of this node.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the start position of this node in the source text.
    /// </summary>
    int StartPosition { get; }

    /// <summary>
    /// Gets the end position of this node in the source text.
    /// </summary>
    int EndPosition { get; }

    /// <summary>
    /// Gets the line number where this node starts.
    /// </summary>
    int StartLine { get; }

    /// <summary>
    /// Gets the column number where this node starts.
    /// </summary>
    int StartColumn { get; }

    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    IReadOnlyList<ISyntaxNode> Children { get; }

    /// <summary>
    /// Gets the parent node, if any.
    /// </summary>
    ISyntaxNode? Parent { get; }

    /// <summary>
    /// Gets whether this node is a terminal (leaf) node.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets whether this node represents an error.
    /// </summary>
    bool IsError { get; }

    /// <summary>
    /// Finds the first child node of the specified type.
    /// </summary>
    /// <param name="nodeType">The type of node to find.</param>
    /// <returns>The first matching child node, or null if not found.</returns>
    ISyntaxNode? FindChild(string nodeType);

    /// <summary>
    /// Finds all child nodes of the specified type.
    /// </summary>
    /// <param name="nodeType">The type of nodes to find.</param>
    /// <returns>All matching child nodes.</returns>
    IEnumerable<ISyntaxNode> FindChildren(string nodeType);

    /// <summary>
    /// Traverses the tree depth-first and yields all descendant nodes.
    /// </summary>
    /// <returns>All descendant nodes.</returns>
    IEnumerable<ISyntaxNode> DescendantNodes();
}
