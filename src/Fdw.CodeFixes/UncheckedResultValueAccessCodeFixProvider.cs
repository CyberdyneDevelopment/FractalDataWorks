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
using Microsoft.CodeAnalysis.Formatting;

namespace Fdw.CodeFixes;

/// <summary>
/// Code fix provider that wraps unguarded IGenericResult&lt;T&gt;.Value access in an IsSuccess check.
/// Generates an if/else block with ErrorMessage handling in the failure path.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UncheckedResultValueAccessCodeFixProvider)), Shared]
public class UncheckedResultValueAccessCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add IsSuccess check before .Value access";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW016");

    /// <summary>
    /// Gets the fix all provider for this code fix provider.
    /// </summary>
    /// <returns>The fix all provider.</returns>
    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the specified context.
    /// </summary>
    /// <param name="context">The code fix context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        // Find the MemberAccessExpressionSyntax for the .Value access
        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(ma => string.Equals(ma.Name.Identifier.Text, "Value", StringComparison.Ordinal));
        if (memberAccess == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => WrapInIsSuccessCheck(context.Document, memberAccess, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> WrapInIsSuccessCheck(
        Document document,
        MemberAccessExpressionSyntax valueAccess,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Get the expression text (variable name before .Value)
        var expressionText = valueAccess.Expression.ToString();

        // Find the containing statement to wrap
        var containingStatement = valueAccess.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
        if (containingStatement == null) return document;

        // Build the if-condition: expr.IsSuccess
        var condition = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ParseExpression(expressionText),
            SyntaxFactory.IdentifierName("IsSuccess"));

        // Then-block: the original statement
        var thenBlock = SyntaxFactory.Block(
            containingStatement.WithoutLeadingTrivia().WithoutTrailingTrivia());

        // Else-block: ErrorMessage = expr.CurrentMessage ?? "Operation failed";
        var errorAssignment = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("ErrorMessage"),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseExpression(expressionText),
                        SyntaxFactory.IdentifierName("CurrentMessage")),
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("Operation failed")))));

        var elseBlock = SyntaxFactory.Block(errorAssignment);

        // Build the full if/else statement
        var ifStatement = SyntaxFactory.IfStatement(
                condition,
                thenBlock,
                SyntaxFactory.ElseClause(elseBlock))
            .WithLeadingTrivia(containingStatement.GetLeadingTrivia())
            .WithTrailingTrivia(containingStatement.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(containingStatement, ifStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
