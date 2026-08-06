using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Fdw.Conventions.CodeFixes;

/// <summary>
/// Code fix provider that removes underscores from method names and updates all references.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveMethodNameUnderscoreCodeFixProvider)), Shared]
public class RemoveMethodNameUnderscoreCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "RemoveMethodNameUnderscore";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW008");

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

        var methodDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null)
            return;

        var methodName = methodDeclaration.Identifier.Text;
        var newName = RemoveUnderscores(methodName);

        if (string.IsNullOrEmpty(newName) || string.Equals(newName, methodName, StringComparison.Ordinal))
            return;

        var title = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "Rename '{0}' to '{1}' and update all references",
            methodName,
            newName);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedSolution: c => RenameMethod(context.Document, methodDeclaration, newName, c),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    private static async Task<Solution> RenameMethod(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        string newName,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document.Project.Solution;

        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (methodSymbol == null)
            return document.Project.Solution;

        var solution = document.Project.Solution;
        var newSolution = await Renamer.RenameSymbolAsync(
            solution,
            methodSymbol,
            default,
            newName,
            cancellationToken).ConfigureAwait(false);

        return newSolution;
    }

    internal static string RemoveUnderscores(string name)
    {
        if (string.IsNullOrEmpty(name) || !name.Contains("_"))
            return name;

        var sb = new StringBuilder(name.Length);
        var capitalizeNext = false;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '_')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0 && char.IsLower(sb[0]))
        {
            sb[0] = char.ToUpperInvariant(sb[0]);
        }

        return sb.Length > 0 ? sb.ToString() : name;
    }
}
