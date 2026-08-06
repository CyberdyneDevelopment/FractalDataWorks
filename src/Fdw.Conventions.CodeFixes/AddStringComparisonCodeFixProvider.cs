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
/// Code fix provider that adds StringComparison argument to string methods.
/// Fixes MA0006 (Use string.Equals instead of == operator / add StringComparison).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddStringComparisonCodeFixProvider)), Shared]
public class AddStringComparisonCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "AddStringComparison";
    private const string DefaultComparison = "Ordinal";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("MA0006");

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

        var invocation = root.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation == null)
            return;

        // Determine the comparison type from MSBuild property
        var comparison = DefaultComparison;
        var analyzerConfigOptions = context.Document.Project.AnalyzerOptions?.AnalyzerConfigOptionsProvider;
        if (analyzerConfigOptions != null &&
            analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.FDW_DefaultStringComparison", out var configValue) &&
            !string.IsNullOrEmpty(configValue))
        {
            comparison = configValue;
        }

        var title = $"Add StringComparison.{comparison}";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => AddStringComparison(context.Document, invocation, comparison, c),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    private static async Task<Document> AddStringComparison(
        Document document,
        InvocationExpressionSyntax invocation,
        string comparison,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Create StringComparison.{comparison} argument
        var comparisonArg = SyntaxFactory.Argument(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("StringComparison"),
                SyntaxFactory.IdentifierName(comparison)));

        // Add to existing argument list
        var newArgList = invocation.ArgumentList.AddArguments(comparisonArg);
        var newInvocation = invocation.WithArgumentList(newArgList);

        var newRoot = root.ReplaceNode(invocation, newInvocation);

        // Add using System; if needed
        var compilationUnit = (CompilationUnitSyntax)newRoot;
        if (!compilationUnit.Usings.Any(u =>
            string.Equals(u.Name?.ToString(), "System", StringComparison.Ordinal)))
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("System"))
                .WithTrailingTrivia(SyntaxFactory.ElasticLineFeed);
            newRoot = compilationUnit.AddUsings(usingDirective);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
