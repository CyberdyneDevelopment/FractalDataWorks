using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.CSharp.Parsing;

/// <summary>
/// Roslyn implementation of ISyntaxNode.
/// </summary>
public sealed class RoslynSyntaxNode : ISyntaxNode
{
    private readonly SyntaxNode _node;
    private readonly string _sourceText;
    private readonly RoslynSyntaxNode? _parent;
    private List<ISyntaxNode>? _children;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynSyntaxNode"/> class.
    /// </summary>
    /// <param name="node">The Roslyn syntax node.</param>
    /// <param name="sourceText">The source text.</param>
    /// <param name="parent">The parent node.</param>
    public RoslynSyntaxNode(SyntaxNode node, string sourceText, RoslynSyntaxNode? parent = null)
    {
        _node = node;
        _sourceText = sourceText;
        _parent = parent;
    }

    /// <inheritdoc/>
    public string NodeType => _node.Kind().ToString();

    /// <inheritdoc/>
    public string Text => _node.ToString();

    /// <inheritdoc/>
    public int StartPosition => _node.SpanStart;

    /// <inheritdoc/>
    public int EndPosition => _node.Span.End;

    /// <inheritdoc/>
    public int StartLine
    {
        get
        {
            var lineSpan = _node.GetLocation().GetLineSpan();
            return lineSpan.StartLinePosition.Line;
        }
    }

    /// <inheritdoc/>
    public int StartColumn
    {
        get
        {
            var lineSpan = _node.GetLocation().GetLineSpan();
            return lineSpan.StartLinePosition.Character;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ISyntaxNode> Children
    {
        get
        {
            if (_children == null)
            {
                _children = [];
                foreach (var child in _node.ChildNodes())
                {
                    _children.Add(new RoslynSyntaxNode(child, _sourceText, this));
                }
            }
            return _children;
        }
    }

    /// <inheritdoc/>
    public ISyntaxNode? Parent => _parent;

    /// <inheritdoc/>
    public bool IsTerminal => !_node.ChildNodes().Any();

    /// <inheritdoc/>
    public bool IsError => _node.IsMissing || _node.ContainsDiagnostics;

    /// <inheritdoc/>
    public ISyntaxNode? FindChild(string nodeType)
    {
        return Children.FirstOrDefault(c => string.Equals(c.NodeType, nodeType, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public IEnumerable<ISyntaxNode> FindChildren(string nodeType)
    {
        return Children.Where(c => string.Equals(c.NodeType, nodeType, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public IEnumerable<ISyntaxNode> DescendantNodes()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.DescendantNodes())
            {
                yield return descendant;
            }
        }
    }
}
