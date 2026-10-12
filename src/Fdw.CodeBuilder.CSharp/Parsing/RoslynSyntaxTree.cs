using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.CSharp.Parsing;

/// <summary>
/// Roslyn implementation of ISyntaxTree.
/// </summary>
public sealed class RoslynSyntaxTree : ISyntaxTree
{
    private readonly SyntaxTree _tree;
    private readonly RoslynSyntaxNode _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynSyntaxTree"/> class.
    /// </summary>
    /// <param name="tree">The Roslyn syntax tree.</param>
    /// <param name="sourceText">The source text.</param>
    /// <param name="language">The language.</param>
    /// <param name="filePath">The file path.</param>
    public RoslynSyntaxTree(SyntaxTree tree, string sourceText, string language, string? filePath = null)
    {
        _tree = tree;
        SourceText = sourceText;
        Language = language;
        FilePath = filePath ?? tree.FilePath;
        _root = new RoslynSyntaxNode(_tree.GetRoot(), sourceText);
    }

    /// <inheritdoc/>
    public ISyntaxNode Root => _root;

    /// <inheritdoc/>
    public string SourceText { get; }

    /// <inheritdoc/>
    public string Language { get; }

    /// <inheritdoc/>
    public string? FilePath { get; }

    /// <inheritdoc/>
    public bool HasErrors => _tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc/>
    public IEnumerable<ISyntaxNode> GetErrors()
    {
        var errorNodes = new List<ISyntaxNode>();
        var root = _tree.GetRoot();

        foreach (var diagnostic in _tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node != null)
            {
                errorNodes.Add(new RoslynSyntaxNode(node, SourceText));
            }
        }

        return errorNodes;
    }

    /// <inheritdoc/>
    public IEnumerable<ISyntaxNode> FindNodes(string nodeType)
    {
        return Root.DescendantNodes().Where(n => string.Equals(n.NodeType, nodeType, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public ISyntaxNode? GetNodeAtPosition(int position)
    {
        if (position < 0 || position >= _tree.Length)
            return null;

        var root = _tree.GetRoot();
        var node = root.FindToken(position).Parent;

        if (node != null)
        {
            // Find the deepest node containing this position
            while (true)
            {
                var child = node.ChildNodes().FirstOrDefault(c => c.Span.Contains(position));
                if (child == null)
                    break;
                node = child;
            }

            return new RoslynSyntaxNode(node, SourceText);
        }

        return null;
    }

    /// <inheritdoc/>
    public ISyntaxNode? GetNodeAtLocation(int line, int column)
    {
        var root = _tree.GetRoot();
        var text = _tree.GetText();
        var position = text.Lines[line].Start + column;

        return GetNodeAtPosition(position);
    }
}
