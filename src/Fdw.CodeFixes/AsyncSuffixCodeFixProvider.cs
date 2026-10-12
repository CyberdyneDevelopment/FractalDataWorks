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
using Microsoft.CodeAnalysis.Rename;

namespace Fdw.CodeFixes;

/// <summary>
/// Code fix provider that removes the 'Async' suffix from method names and updates all references.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncSuffixCodeFixProvider)), Shared]
public class AsyncSuffixCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove 'Async' suffix and update all references";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW001");

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

        // Find the method declaration
        var methodDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();
        if (methodDeclaration == null) return;

        var methodName = methodDeclaration.Identifier.Text;
        var newName = methodName.Substring(0, methodName.Length - 5);

        if (string.IsNullOrEmpty(newName)) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: string.Format(System.Globalization.CultureInfo.InvariantCulture, "Rename '{0}' to '{1}' and update all references", methodName, newName),
                createChangedSolution: c => RenameMethodAsync(context.Document, methodDeclaration, newName, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Solution> RenameMethodAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        string newName,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return document.Project.Solution;

        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (methodSymbol == null) return document.Project.Solution;

        // Use Roslyn's rename service to rename the method and all its references
        var solution = document.Project.Solution;
        var newSolution = await Renamer.RenameSymbolAsync(
            solution,
            methodSymbol,
            default,
            newName,
            cancellationToken).ConfigureAwait(false);

        return newSolution;
    }
}
