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

namespace Fdw.Conventions.CodeFixes;

/// <summary>
/// FDW055 code fix: rewrites a <c>?? ""</c> fallback to <c>?? string.Empty</c>, and an empty collection spelled long
/// (<c>Array.Empty&lt;T&gt;()</c>, <c>Enumerable.Empty&lt;T&gt;()</c>, <c>new List&lt;T&gt;()</c>) to <c>[]</c>.
/// </summary>
/// <remarks>
/// Only the diagnostics that carry the Fix property are fixable; every other FDW055 is a
/// fallback that fails loud, which no mechanical rewrite can do (FDW-3580).
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoFallbackCodeFixProvider)), Shared]
public sealed class NoFallbackCodeFixProvider : CodeFixProvider
{
    // NoFallbackAnalyzer's DiagnosticId, FixProperty, StringEmptyFix and EmptyCollectionFix in Fdw.Conventions.Analyzers;
    // this project does not reference that one, so the values are repeated here and must match.
    private const string DiagnosticId = "FDW055";
    private const string FixProperty = "Fix";
    private const string StringEmptyFix = "StringEmpty";
    private const string EmptyCollectionFix = "EmptyCollection";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(FixProperty, out var fix) || fix is null)
                continue;

            var right = RightSide(root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true));
            if (right is null)
                continue;

            var replacement = Replacement(fix);
            if (replacement is null)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title(fix),
                    cancellationToken => Replace(context.Document, root, right, replacement, cancellationToken),
                    equivalenceKey: fix),
                diagnostic);
        }
    }

    private static ExpressionSyntax? RightSide(SyntaxNode node) =>
        node.AncestorsAndSelf().Select(n => n switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression) => binary.Right,
            AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression) => assignment.Right,
            _ => null,
        }).FirstOrDefault(right => right is not null);

    private static ExpressionSyntax? Replacement(string fix) => fix switch
    {
        StringEmptyFix => SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
            SyntaxFactory.IdentifierName("Empty")),
        EmptyCollectionFix => SyntaxFactory.CollectionExpression(),
        _ => null,
    };

    private static string Title(string fix) => fix switch
    {
        StringEmptyFix => "Write string.Empty",
        _ => "Write []",
    };

    private static Task<Document> Replace(
        Document document,
        SyntaxNode root,
        ExpressionSyntax right,
        ExpressionSyntax replacement,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(right, replacement.WithTriviaFrom(right))));
    }
}
