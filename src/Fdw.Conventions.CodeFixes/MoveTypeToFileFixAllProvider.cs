using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Conventions.CodeFixes;

/// <summary>
/// Custom FixAllProvider that processes all FDW005/MA0048 diagnostics per-document in a single pass.
/// This avoids span invalidation issues that occur when BatchFixer removes types one-at-a-time
/// from the same source file.
/// </summary>
internal sealed class MoveTypeToFileFixAllProvider : FixAllProvider
{
    internal static readonly MoveTypeToFileFixAllProvider Instance = new();

    /// <inheritdoc/>
    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        switch (fixAllContext.Scope)
        {
            case FixAllScope.Document:
                return CodeAction.Create(
                    "Move all misnamed types to their own files (document)",
                    async ct =>
                    {
                        if (fixAllContext.Document == null)
                            return fixAllContext.Solution;

                        return await FixDocumentAsync(
                            fixAllContext.Solution, fixAllContext.Document, fixAllContext).ConfigureAwait(false);
                    },
                    nameof(MoveTypeToFileFixAllProvider));

            case FixAllScope.Project:
                return CodeAction.Create(
                    "Move all misnamed types to their own files (project)",
                    async ct =>
                    {
                        var solution = fixAllContext.Solution;
                        var project = fixAllContext.Project;

                        // Get all diagnostics for the project, grouped by document
                        var diagnosticsByDoc = await GetDiagnosticsByDocumentAsync(
                            fixAllContext, project).ConfigureAwait(false);

                        foreach (var kvp in diagnosticsByDoc)
                        {
                            var document = solution.GetDocument(kvp.Key);
                            if (document == null)
                                continue;

                            solution = await FixDocumentFromDiagnosticsAsync(
                                solution, document, kvp.Value).ConfigureAwait(false);
                        }

                        return solution;
                    },
                    nameof(MoveTypeToFileFixAllProvider));

            case FixAllScope.Solution:
                return CodeAction.Create(
                    "Move all misnamed types to their own files (solution)",
                    async ct =>
                    {
                        var solution = fixAllContext.Solution;

                        foreach (var project in solution.Projects)
                        {
                            var diagnosticsByDoc = await GetDiagnosticsByDocumentAsync(
                                fixAllContext, project).ConfigureAwait(false);

                            foreach (var kvp in diagnosticsByDoc)
                            {
                                var document = solution.GetDocument(kvp.Key);
                                if (document == null)
                                    continue;

                                solution = await FixDocumentFromDiagnosticsAsync(
                                    solution, document, kvp.Value).ConfigureAwait(false);
                            }
                        }

                        return solution;
                    },
                    nameof(MoveTypeToFileFixAllProvider));

            default:
                return null;
        }
    }

    private static async Task<Solution> FixDocumentAsync(
        Solution solution, Document document, FixAllContext fixAllContext)
    {
        var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
        if (diagnostics.IsEmpty)
            return solution;

        // Re-resolve document from current solution
        document = solution.GetDocument(document.Id) ?? document;
        return await FixDocumentFromDiagnosticsAsync(solution, document, diagnostics).ConfigureAwait(false);
    }

    private static async Task<Solution> FixDocumentFromDiagnosticsAsync(
        Solution solution,
        Document document,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
            return solution;

        // Re-resolve document from current solution
        document = solution.GetDocument(document.Id) ?? document;

        var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
        if (root == null)
            return solution;

        // Find ALL type declarations for all diagnostics in this document in one pass
        var typeDeclarations = new List<BaseTypeDeclarationSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var typeDecl = MoveTypeToFileCodeFixProvider.FindTypeDeclaration(
                root, diagnostic.Location.SourceSpan.Start);

            if (typeDecl != null)
                typeDeclarations.Add(typeDecl);
        }

        if (typeDeclarations.Count == 0)
            return solution;

        // Process all types from this document in a single pass
        return await MoveTypeToFileCodeFixProvider.MoveTypesToFiles(
            document, typeDeclarations, default).ConfigureAwait(false);
    }

    private static async Task<Dictionary<DocumentId, List<Diagnostic>>> GetDiagnosticsByDocumentAsync(
        FixAllContext fixAllContext, Project project)
    {
        var result = new Dictionary<DocumentId, List<Diagnostic>>();

        foreach (var document in project.Documents)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            if (!diagnostics.IsEmpty)
            {
                result[document.Id] = diagnostics.ToList();
            }
        }

        return result;
    }
}
