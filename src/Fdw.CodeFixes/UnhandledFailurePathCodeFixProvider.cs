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
/// Code fix provider that adds an else clause for unhandled GenericResult failure paths.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnhandledFailurePathCodeFixProvider)), Shared]
public class UnhandledFailurePathCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add failure path handling";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW013");

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

        var ifStatement = node.AncestorsAndSelf().OfType<IfStatementSyntax>().FirstOrDefault();
        if (ifStatement == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddElseClauseAsync(context.Document, ifStatement, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> AddElseClauseAsync(
        Document document,
        IfStatementSyntax ifStatement,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Extract the variable name from the condition
        var variableName = ExtractVariableName(ifStatement.Condition);
        if (string.IsNullOrEmpty(variableName))
        {
            variableName = "result";
        }

        // Determine if we're in a loop (can't return from loops)
        var isInLoop = ifStatement.Ancestors().Any(a =>
            a is ForStatementSyntax || a is ForEachStatementSyntax ||
            a is WhileStatementSyntax || a is DoStatementSyntax);

        ElseClauseSyntax elseClause;

        if (isInLoop)
        {
            // In a loop, add a TODO comment
            elseClause = SyntaxFactory.ElseClause(
                SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("// TODO: handle " + variableName + ".IsFailure\n")));
        }
        else
        {
            // Create: else if (result.IsFailure) { return GenericResult.Failure(result.Messages.ToArray()); }
            var failureCondition = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(variableName!),
                SyntaxFactory.IdentifierName("IsFailure"));

            var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("GenericResult"),
                        SyntaxFactory.IdentifierName("Failure")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(variableName!),
                                            SyntaxFactory.IdentifierName("Messages")),
                                        SyntaxFactory.IdentifierName("ToArray"))))))))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            elseClause = SyntaxFactory.ElseClause(
                SyntaxFactory.IfStatement(
                    failureCondition,
                    SyntaxFactory.Block(returnStatement)));
        }

        var newIfStatement = ifStatement
            .WithElse(elseClause)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(ifStatement, newIfStatement);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string? ExtractVariableName(ExpressionSyntax condition)
    {
        // Handle: result.IsSuccess
        if (condition is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is IdentifierNameSyntax identifier)
                return identifier.Identifier.Text;
        }

        // Handle: !result.IsFailure or !result.IsSuccess
        if (condition is PrefixUnaryExpressionSyntax prefix &&
            prefix.Kind() == SyntaxKind.LogicalNotExpression)
        {
            return ExtractVariableName(prefix.Operand);
        }

        // Handle: result.IsSuccess && result.Value != null
        if (condition is BinaryExpressionSyntax binary &&
            binary.Kind() == SyntaxKind.LogicalAndExpression)
        {
            return ExtractVariableName(binary.Left);
        }

        return null;
    }
}
