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
/// Code fix provider that replaces broken result chain patterns with ToNewResult() or Chain().
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BrokenResultChainCodeFixProvider)), Shared]
public class BrokenResultChainCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use ToNewResult() to preserve result chain";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW015");

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

        var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => ReplaceWithToNewResultAsync(context.Document, invocation, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithToNewResultAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var resultVariableName = FindResultVariable(invocation);
        if (resultVariableName == null) return document;

        var typeArgument = ExtractTypeArgument(invocation);

        ExpressionSyntax replacement;

        if (typeArgument != null)
        {
            // Generic case: result.ToNewResult<T>()
            replacement = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(resultVariableName),
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("ToNewResult"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(typeArgument)))));
        }
        else
        {
            // Non-generic case: just use the result variable directly
            // since IGenericResult<T> is assignable to IGenericResult
            replacement = SyntaxFactory.IdentifierName(resultVariableName);
        }

        replacement = replacement
            .WithLeadingTrivia(invocation.GetLeadingTrivia())
            .WithTrailingTrivia(invocation.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(invocation, replacement);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Extracts the type argument T from GenericResult&lt;T&gt;.Failure(...).
    /// Returns null if the invocation is on non-generic GenericResult.
    /// </summary>
    private static TypeSyntax? ExtractTypeArgument(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        // GenericResult<T>.Failure — Expression is GenericNameSyntax
        if (memberAccess.Expression is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.Count == 1)
        {
            return genericName.TypeArgumentList.Arguments[0];
        }

        // Namespace.GenericResult<T>.Failure — Expression is QualifiedNameSyntax
        if (memberAccess.Expression is QualifiedNameSyntax qualifiedName &&
            qualifiedName.Right is GenericNameSyntax rightGeneric &&
            rightGeneric.TypeArgumentList.Arguments.Count == 1)
        {
            return rightGeneric.TypeArgumentList.Arguments[0];
        }

        return null;
    }

    private static string? FindResultVariable(InvocationExpressionSyntax invocation)
    {
        // Look through arguments for member access on a result variable
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var expr = argument.Expression;

            // Handle: result.Messages.ToArray()
            if (expr is InvocationExpressionSyntax innerInvocation &&
                innerInvocation.Expression is MemberAccessExpressionSyntax toArrayAccess &&
                toArrayAccess.Expression is MemberAccessExpressionSyntax messagesAccess &&
                messagesAccess.Expression is IdentifierNameSyntax toArrayIdentifier)
            {
                return toArrayIdentifier.Identifier.Text;
            }

            // Handle: result.Messages, result.Code, result.Details
            if (expr is MemberAccessExpressionSyntax directAccess &&
                directAccess.Expression is IdentifierNameSyntax directIdentifier)
            {
                return directIdentifier.Identifier.Text;
            }

            // Handle: result.Code ?? fallback (null-coalescing)
            if (expr is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.CoalesceExpression) &&
                binary.Left is MemberAccessExpressionSyntax coalesceAccess &&
                coalesceAccess.Expression is IdentifierNameSyntax coalesceIdentifier)
            {
                return coalesceIdentifier.Identifier.Text;
            }
        }

        return null;
    }
}
