using System;
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
/// Code fix provider that adds ConfigureAwait(false) to awaited expressions.
/// Fixes AsyncFixer04.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddConfigureAwaitCodeFixProvider)), Shared]
public class AddConfigureAwaitCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add ConfigureAwait(false)";
    private const string EquivalenceKey = "AddConfigureAwait";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("AsyncFixer04");

    /// <summary>
    /// Gets the fix all provider.
    /// </summary>
    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the specified context.
    /// </summary>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var awaitExpression = root.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<AwaitExpressionSyntax>()
            .FirstOrDefault();

        if (awaitExpression == null)
            return;

        // Skip if already has ConfigureAwait
        if (HasConfigureAwait(awaitExpression.Expression))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddConfigureAwait(context.Document, awaitExpression, c),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    private static async Task<Document> AddConfigureAwait(
        Document document,
        AwaitExpressionSyntax awaitExpression,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var expression = awaitExpression.Expression;

        // Wrap: expr -> expr.ConfigureAwait(false)
        var configureAwait = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression.WithoutTrivia(),
                SyntaxFactory.IdentifierName("ConfigureAwait")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))));

        var newAwait = awaitExpression
            .WithExpression(configureAwait)
            .WithTriviaFrom(awaitExpression);

        var newRoot = root.ReplaceNode(awaitExpression, newAwait);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool HasConfigureAwait(ExpressionSyntax expression)
    {
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return string.Equals(memberAccess.Name.Identifier.Text, "ConfigureAwait", StringComparison.Ordinal);
        }

        return false;
    }
}
