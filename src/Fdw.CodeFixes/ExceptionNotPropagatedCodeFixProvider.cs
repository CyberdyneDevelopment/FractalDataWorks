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
/// Code fix provider that wraps catch block logging calls in GenericResult.Failure() returns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionNotPropagatedCodeFixProvider)), Shared]
public class ExceptionNotPropagatedCodeFixProvider : CodeFixProvider
{
    private const string Title = "Propagate exception in GenericResult";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW014");

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

        var catchClause = node.AncestorsAndSelf().OfType<CatchClauseSyntax>().FirstOrDefault();
        if (catchClause == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => PropagateExceptionAsync(context.Document, catchClause, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> PropagateExceptionAsync(
        Document document,
        CatchClauseSyntax catchClause,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var block = catchClause.Block;
        if (block == null) return document;

        // Check if catch block has no return - need to add one
        var hasReturn = block.Statements.Any(s => s is ReturnStatementSyntax);

        if (!hasReturn)
        {
            return FixNoReturnCatchBlock(document, root, catchClause, block);
        }

        // Has return but returns Success without messages - add message parameter
        return FixSuccessWithoutMessageCatchBlock(document, root, catchClause, block);
    }

    private static Document FixNoReturnCatchBlock(
        Document document,
        SyntaxNode root,
        CatchClauseSyntax catchClause,
        BlockSyntax block)
    {
        // Find the last expression statement (likely a logging call)
        var lastExprStatement = block.Statements
            .OfType<ExpressionStatementSyntax>()
            .LastOrDefault();

        if (lastExprStatement != null)
        {
            // Wrap the logging call in a return GenericResult.Failure(...)
            // Before: DomainLog.OperationFailed(_logger, ex, context);
            // After: return GenericResult<T>.Failure(DomainLog.OperationFailed(_logger, ex, context));
            var loggingExpression = lastExprStatement.Expression;

            var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("GenericResult"),
                        SyntaxFactory.IdentifierName("Failure")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(loggingExpression.WithoutTrivia())))))
                .WithLeadingTrivia(lastExprStatement.GetLeadingTrivia())
                .WithTrailingTrivia(lastExprStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newBlock = block.ReplaceNode(lastExprStatement, returnStatement);
            var newCatch = catchClause.WithBlock(newBlock);
            var newRoot = root.ReplaceNode(catchClause, newCatch);
            return document.WithSyntaxRoot(newRoot);
        }

        // No expression statement found - add a stub return
        var stubReturn = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("GenericResult"),
                    SyntaxFactory.IdentifierName("Failure")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.ParseExpression("/* TODO: add MessageLogging method */"))))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var blockWithReturn = block.AddStatements(stubReturn);
        var catchWithReturn = catchClause.WithBlock(blockWithReturn);
        var rootWithReturn = root.ReplaceNode(catchClause, catchWithReturn);
        return document.WithSyntaxRoot(rootWithReturn);
    }

    private static Document FixSuccessWithoutMessageCatchBlock(
        Document document,
        SyntaxNode root,
        CatchClauseSyntax catchClause,
        BlockSyntax block)
    {
        // Find the return statement that returns Success without messages
        var returnStatement = block.Statements
            .OfType<ReturnStatementSyntax>()
            .FirstOrDefault();

        if (returnStatement?.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            string.Equals(memberAccess.Name.Identifier.Text, "Success", System.StringComparison.Ordinal))
        {
            // Find a logging call in the block to use as the message
            var loggingCall = block.Statements
                .OfType<ExpressionStatementSyntax>()
                .Select(s => s.Expression)
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (loggingCall != null)
            {
                // Convert: DomainLog.RetrySucceeded(_logger, ex, attempt);
                // To: var retryMessage = DomainLog.RetrySucceeded(_logger, ex, attempt);
                // And add retryMessage as second arg to Success
                var variableName = "exceptionMessage";

                // Create variable declaration for the logging call
                var loggingStatement = block.Statements
                    .OfType<ExpressionStatementSyntax>()
                    .FirstOrDefault(s => s.Expression == loggingCall);

                if (loggingStatement != null)
                {
                    var varDeclaration = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("var"))
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(variableName))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(loggingCall.WithoutTrivia())))))
                        .WithLeadingTrivia(loggingStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(loggingStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);

                    // Add the message variable as argument to Success
                    var newArgs = invocation.ArgumentList.Arguments.Add(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variableName)));

                    var newInvocation = invocation.WithArgumentList(
                        SyntaxFactory.ArgumentList(newArgs));

                    var newReturn = returnStatement.WithExpression(newInvocation);

                    var newBlock = block
                        .ReplaceNode(loggingStatement, varDeclaration)
                        .ReplaceNode(
                            block.Statements.OfType<ReturnStatementSyntax>().First(),
                            newReturn);

                    // Need to rebuild the block properly since ReplaceNode doesn't chain well
                    var statements = block.Statements.ToList();
                    var loggingIndex = statements.IndexOf(loggingStatement);
                    var returnIndex = statements.IndexOf(returnStatement);

                    statements[loggingIndex] = varDeclaration;
                    statements[returnIndex] = newReturn;

                    var rebuiltBlock = SyntaxFactory.Block(statements);
                    var newCatch = catchClause.WithBlock(rebuiltBlock);
                    var newRoot = root.ReplaceNode(catchClause, newCatch);
                    return document.WithSyntaxRoot(newRoot);
                }
            }
        }

        return document;
    }
}
