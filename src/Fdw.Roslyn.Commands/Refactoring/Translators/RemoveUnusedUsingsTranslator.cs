using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for RemoveUnusedUsingsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RemoveUnusedUsings")]
public sealed class RemoveUnusedUsingsTranslator : RoslynCommandTranslatorBase<RemoveUnusedUsingsCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveUnusedUsingsTranslator"/> class.
    /// </summary>
    public RemoveUnusedUsingsTranslator()
        : base("RemoveUnusedUsings", "Removes unused using directives from a file")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: detect unused usings via diagnostics and usage analysis
    public override async Task<IGenericResult<MutationResult>> Translate(
        RemoveUnusedUsingsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);

        // CS8019: Unnecessary using directive
        // IDE0005: Remove unnecessary import
        var unusedUsingDiagnostics = diagnostics
            .Where(d => string.Equals(d.Id, "CS8019", StringComparison.Ordinal) ||
                        string.Equals(d.Id, "IDE0005", StringComparison.Ordinal))
            .ToList();

        var unusedUsings = new HashSet<UsingDirectiveSyntax>();

        foreach (var diagnostic in unusedUsingDiagnostics)
        {
            var location = diagnostic.Location;
            if (!location.IsInSource)
                continue;

            var span = location.SourceSpan;
            var node = syntaxRoot.FindNode(span);

            if (node is UsingDirectiveSyntax usingDirective)
            {
                unusedUsings.Add(usingDirective);
            }
        }

        // Also check using directives manually by analyzing symbol usage
        var allUsings = syntaxRoot.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name is not null)
            .ToList();

        var usedNamespaces = new HashSet<string>(StringComparer.Ordinal);

        // Find all type references and their namespaces
        foreach (var node in syntaxRoot.DescendantNodes())
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            var symbol = symbolInfo.Symbol;

            if (symbol is INamedTypeSymbol typeSymbol && typeSymbol.ContainingNamespace is not null)
            {
                usedNamespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
            }
        }

        // Find usings that aren't in the used set
        foreach (var usingDir in allUsings)
        {
            var namespaceName = usingDir.Name?.ToString();
            if (namespaceName is null)
                continue;

            if (!usedNamespaces.Contains(namespaceName))
            {
                unusedUsings.Add(usingDir);
            }
        }

        if (unusedUsings.Count == 0)
        {
            return GenericResult<MutationResult>.Success(
                new MutationResult(
                    "No unused using directives found",
                    solution));
        }

        // Remove unused usings
        var newRoot = syntaxRoot.RemoveNodes(unusedUsings, SyntaxRemoveOptions.KeepNoTrivia)!;
        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = unusedUsings.Count
            }
        };

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Removed {unusedUsings.Count} unused using directives",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051
}
