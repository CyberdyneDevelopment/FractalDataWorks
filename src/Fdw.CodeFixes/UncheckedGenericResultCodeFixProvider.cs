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
/// Code fix provider that adds failure checking for unchecked GenericResult values.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UncheckedGenericResultCodeFixProvider)), Shared]
public class UncheckedGenericResultCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add GenericResult failure check";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW012");

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

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddFailureCheckAsync(context.Document, node, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> AddFailureCheckAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return document;

        // Find the statement containing the diagnostic
        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (statement == null) return document;

        if (statement is ExpressionStatementSyntax expressionStatement)
        {
            return await FixExpressionStatementAsync(document, root, expressionStatement, semanticModel, cancellationToken).ConfigureAwait(false);
        }

        if (statement is LocalDeclarationStatementSyntax localDeclaration)
        {
            return await FixLocalDeclarationAsync(document, root, localDeclaration, semanticModel, cancellationToken).ConfigureAwait(false);
        }

        return document;
    }

    private static Task<Document> FixExpressionStatementAsync(
        Document document,
        SyntaxNode root,
        ExpressionStatementSyntax expressionStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Derive variable name from method name
        var variableName = DeriveVariableName(expressionStatement.Expression);

        // Create: var xResult = <expression>;
        var declaration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var"))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(variableName))
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            expressionStatement.Expression.WithoutTrivia())))))
            .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Create failure check
        var failureCheck = CreateFailureCheckStatement(variableName, semanticModel, expressionStatement);

        var newStatements = new SyntaxNode[] { declaration, failureCheck };
        var newRoot = root.ReplaceNode(expressionStatement, newStatements);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> FixLocalDeclarationAsync(
        Document document,
        SyntaxNode root,
        LocalDeclarationStatementSyntax localDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var variableName = localDeclaration.Declaration.Variables.FirstOrDefault()?.Identifier.Text;
        if (string.IsNullOrEmpty(variableName)) return Task.FromResult(document);

        // For discard assignment, replace _ with a real variable name
        if (string.Equals(variableName, "_", System.StringComparison.Ordinal))
        {
            var initializer = localDeclaration.Declaration.Variables.First().Initializer?.Value;
            variableName = DeriveVariableName(initializer);

            var newDeclaration = localDeclaration.WithDeclaration(
                localDeclaration.Declaration.WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        localDeclaration.Declaration.Variables.First()
                            .WithIdentifier(SyntaxFactory.Identifier(variableName!)))));

            var failureCheck = CreateFailureCheckStatement(variableName!, semanticModel, localDeclaration);

            var newStatements = new SyntaxNode[] { newDeclaration, failureCheck };
            var newRoot = root.ReplaceNode(localDeclaration, newStatements);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        // For assigned-but-never-checked: insert failure check after declaration
        var check = CreateFailureCheckStatement(variableName!, semanticModel, localDeclaration);
        var rootWithCheck = root.InsertNodesAfter(localDeclaration, new[] { check });

        return Task.FromResult(document.WithSyntaxRoot(rootWithCheck));
    }

    private static string DeriveVariableName(ExpressionSyntax? expression)
    {
        // Unwrap await
        if (expression is AwaitExpressionSyntax awaitExpr)
            expression = awaitExpr.Expression;

        // Unwrap ConfigureAwait
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            string.Equals(memberAccess.Name.Identifier.Text, "ConfigureAwait", System.StringComparison.Ordinal))
        {
            expression = memberAccess.Expression;
        }

        // Get method name
        if (expression is InvocationExpressionSyntax methodCall)
        {
            string? methodName = null;
            if (methodCall.Expression is MemberAccessExpressionSyntax ma)
                methodName = ma.Name.Identifier.Text;
            else if (methodCall.Expression is IdentifierNameSyntax id)
                methodName = id.Identifier.Text;

            if (!string.IsNullOrEmpty(methodName))
            {
                // Convert PascalCase method name to camelCase + "Result"
                var camel = char.ToLowerInvariant(methodName![0]) + methodName.Substring(1);
                return camel + "Result";
            }
        }

        return "result";
    }

    private static IfStatementSyntax CreateFailureCheckStatement(
        string variableName,
        SemanticModel semanticModel,
        SyntaxNode contextNode)
    {
        // if (variableName.IsFailure)
        // {
        //     return GenericResult.Failure(variableName.Messages.ToArray());
        // }
        var condition = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(variableName),
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
                                        SyntaxFactory.IdentifierName(variableName),
                                        SyntaxFactory.IdentifierName("Messages")),
                                    SyntaxFactory.IdentifierName("ToArray"))))))))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return SyntaxFactory.IfStatement(
            condition,
            SyntaxFactory.Block(returnStatement))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }
}
