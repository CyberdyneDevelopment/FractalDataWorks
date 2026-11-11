using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.CodeFixes;

/// <summary>
/// Code fix for FDW063: removes an ArgumentNullException guard on a parameter that cannot be null. A guarding statement (the if
/// that throws, or <c>ArgumentNullException.ThrowIfNull(p);</c>) is removed; <c>p ?? throw new ArgumentNullException(...)</c>
/// becomes <c>p</c>.
/// </summary>
/// <remarks>
/// Fix All removes every guard of a document in one pass, so guards that stand next to each other never make overlapping edits,
/// and then drops the blank line a removed first statement leaves after a block's opening brace.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImpossibleNullGuardCodeFixProvider)), Shared]
public class ImpossibleNullGuardCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove the ArgumentNullException guard";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("FDW063");

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create((fixAllContext, document, diagnostics) => RemoveGuards(document, diagnostics, fixAllContext.CancellationToken));

    /// <inheritdoc/>
    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    cancellationToken => RemoveGuardsOrKeep(context.Document, ImmutableArray.Create(diagnostic), cancellationToken),
                    equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> RemoveGuardsOrKeep(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken) =>
        await RemoveGuards(document, diagnostics, cancellationToken).ConfigureAwait(false) is { } fixedDocument ? fixedDocument : document;

    private static async Task<Document?> RemoveGuards(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
            return null;

        var guards = diagnostics
            .Select(diagnostic => root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true))
            .Select(node => node.AncestorsAndSelf().FirstOrDefault(IsGuard))
            .OfType<SyntaxNode>()
            .Distinct()
            .ToList();

        return guards.Count == 0 ? null : document.WithSyntaxRoot(RemoveGuards(root, guards));
    }

    private static bool IsGuard(SyntaxNode node) =>
        node is IfStatementSyntax or ExpressionStatementSyntax || node.IsKind(SyntaxKind.CoalesceExpression);

    private static SyntaxNode RemoveGuards(SyntaxNode root, IReadOnlyList<SyntaxNode> guards)
    {
        var statements = guards.OfType<StatementSyntax>().ToList();
        var coalesces = guards.OfType<BinaryExpressionSyntax>().ToList();
        var blocks = statements.Select(statement => statement.Parent).OfType<BlockSyntax>().Distinct().ToList();

        var tracked = root.TrackNodes(statements.Cast<SyntaxNode>().Concat(coalesces).Concat(blocks));

        // A coalesce keeps the value it guarded.
        tracked = tracked.ReplaceNodes(
            coalesces.Select(coalesce => tracked.GetCurrentNode(coalesce)).OfType<BinaryExpressionSyntax>(),
            (_, rewritten) => rewritten.Left.WithTriviaFrom(rewritten));

        var currentStatements = statements.Select(statement => tracked.GetCurrentNode(statement)).OfType<StatementSyntax>().ToList();
        if (currentStatements.Count > 0 && tracked.RemoveNodes(currentStatements, SyntaxRemoveOptions.KeepNoTrivia) is { } removed)
            tracked = removed;

        foreach (var block in blocks)
        {
            if (tracked.GetCurrentNode(block) is { Statements.Count: > 0 } current)
            {
                var first = current.Statements[0];
                tracked = tracked.ReplaceNode(first, first.WithLeadingTrivia(WithoutLeadingBlankLines(first.GetLeadingTrivia())));
            }
        }

        return tracked;
    }

    // Drops the blank lines before a block's first statement (whitespace and line ends up to the last line end before anything
    // else), keeping its indentation and any comment.
    private static SyntaxTriviaList WithoutLeadingBlankLines(SyntaxTriviaList trivia)
    {
        var lastLineEnd = -1;
        for (var index = 0; index < trivia.Count; index++)
        {
            if (trivia[index].IsKind(SyntaxKind.EndOfLineTrivia))
                lastLineEnd = index;
            else if (!trivia[index].IsKind(SyntaxKind.WhitespaceTrivia))
                break;
        }

        return lastLineEnd < 0 ? trivia : SyntaxFactory.TriviaList(trivia.Skip(lastLineEnd + 1));
    }
}
