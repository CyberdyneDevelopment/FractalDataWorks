using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Analysis.Helpers;

/// <summary>
/// Reads top-level type declarations and their namespaces out of a document, syntactically.
/// </summary>
public static class TypeDeclarationReader
{
    private static readonly string[] TypeOptionAttributeNames =
    {
        "TypeOption",
        "TypeOptionAttribute",
        "ServiceTypeOption",
        "ServiceTypeOptionAttribute",
    };

    /// <summary>
    /// Reads the top-level type declarations from a document.
    /// </summary>
    /// <param name="document">The document to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The declarations found; empty when the document has none.</returns>
    /// <remarks>
    /// Only TOP-LEVEL types are returned. A nested type shares its parent's file by definition, so it can
    /// never disagree with the path independently of its parent.
    /// </remarks>
    public static async Task<IReadOnlyList<TypeDeclarationInfo>> Read(
        Document document,
        CancellationToken cancellationToken = default)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return Array.Empty<TypeDeclarationInfo>();

        var results = new List<TypeDeclarationInfo>();

        foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Why: a nested type's location is decided by its parent's file, so evaluating it separately
            // would report a phantom mismatch for a file that is already correct.
            if (declaration.Parent is BaseTypeDeclarationSyntax) continue;

            results.Add(new TypeDeclarationInfo(
                NamespaceOf(declaration),
                declaration.Identifier.ValueText,
                HasTypeOptionAttribute(declaration)));
        }

        return results;
    }

    private static string NamespaceOf(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case FileScopedNamespaceDeclarationSyntax fileScoped:
                    return fileScoped.Name.ToString();
                case NamespaceDeclarationSyntax block:
                    return block.Name.ToString();
                default:
                    continue;
            }
        }

        return string.Empty;
    }

    private static bool HasTypeOptionAttribute(BaseTypeDeclarationSyntax declaration) =>
        declaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Select(attribute => attribute.Name.ToString())
            .Select(name => name.Contains('.', StringComparison.Ordinal)
                ? name.Substring(name.LastIndexOf('.') + 1)
                : name)
            .Any(name => Array.Exists(
                TypeOptionAttributeNames,
                known => string.Equals(known, name, StringComparison.Ordinal)));
}
