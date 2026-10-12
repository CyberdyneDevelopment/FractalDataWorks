using System;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
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
/// Code fix provider that adds [ConventionOverride] attribute to suppress FDW006 or FDW007.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SuppressWithConventionOverrideCodeFixProvider)), Shared]
public class SuppressWithConventionOverrideCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW006", "FDW007");

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

        // Find the method/ctor/dtor declaration
        var token = root.FindToken(diagnosticSpan.Start);
        var declaration = token.Parent?.AncestorsAndSelf()
            .OfType<BaseMethodDeclarationSyntax>()
            .FirstOrDefault();

        if (declaration == null)
            return;

        var isFDW006 = string.Equals(diagnostic.Id, "FDW006", StringComparison.Ordinal);
        var propertyName = isFDW006 ? "MaxMethodLines" : "MaxCyclomaticComplexity";

        // Parse the actual count from the diagnostic message args
        // FDW006 message: "Method '{0}' has {1} executable lines (threshold: {2})"
        // FDW007 message: "Method '{0}' has cyclomatic complexity {1} (threshold: {2})"
        int overrideValue;
        if (diagnostic.Properties.TryGetValue("ActualValue", out var actualStr) &&
            int.TryParse(actualStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var actual))
        {
            overrideValue = actual + (isFDW006 ? 20 : 5);
        }
        else
        {
            // Fallback: safe defaults
            overrideValue = isFDW006 ? 100 : 20;
        }

        var title = string.Format(
            CultureInfo.InvariantCulture,
            "Add [ConventionOverride({0} = {1})]",
            propertyName,
            overrideValue);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => AddConventionOverrideAttribute(
                    context.Document, declaration, propertyName, overrideValue, c),
                equivalenceKey: $"ConventionOverride_{diagnostic.Id}"),
            diagnostic);
    }

    private static async Task<Document> AddConventionOverrideAttribute(
        Document document,
        BaseMethodDeclarationSyntax declaration,
        string propertyName,
        int value,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Build the attribute argument: PropertyName = value
        var attributeArgument = SyntaxFactory.AttributeArgument(
            SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(propertyName)),
            null,
            SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(value)));

        var existingAttrLists = declaration.AttributeLists;
        BaseMethodDeclarationSyntax newDeclaration;

        var existingOverride = FindExistingConventionOverride(existingAttrLists);
        if (existingOverride.HasValue)
        {
            // Merge into existing [ConventionOverride(...)]
            var (listIndex, attrIndex) = existingOverride.Value;
            var existingAttr = existingAttrLists[listIndex].Attributes[attrIndex];
            var existingArgList = existingAttr.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
            var newArgList = existingArgList.AddArguments(attributeArgument);
            var newAttr = existingAttr.WithArgumentList(newArgList);

            var updatedAttrList = existingAttrLists[listIndex].WithAttributes(
                existingAttrLists[listIndex].Attributes.Replace(existingAttr, newAttr));

            newDeclaration = declaration.WithAttributeLists(
                existingAttrLists.Replace(existingAttrLists[listIndex], updatedAttrList));
        }
        else
        {
            // Add new [ConventionOverride(PropertyName = value)] attribute
            var attribute = SyntaxFactory.Attribute(
                SyntaxFactory.ParseName("ConventionOverride"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(attributeArgument)));

            var attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute))
                .WithTrailingTrivia(SyntaxFactory.ElasticLineFeed);

            newDeclaration = declaration.WithAttributeLists(
                existingAttrLists.Add(attributeList));
        }

        var newRoot = root.ReplaceNode(declaration, newDeclaration);

        // Add using directive if needed
        var compilationUnit = (CompilationUnitSyntax)newRoot;
        if (!HasUsing(compilationUnit, "Fdw.Conventions"))
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("Fdw.Conventions"))
                .WithTrailingTrivia(SyntaxFactory.ElasticLineFeed);
            newRoot = compilationUnit.AddUsings(usingDirective);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static (int listIndex, int attrIndex)? FindExistingConventionOverride(
        SyntaxList<AttributeListSyntax> attributeLists)
    {
        for (var i = 0; i < attributeLists.Count; i++)
        {
            var attrs = attributeLists[i].Attributes;
            for (var j = 0; j < attrs.Count; j++)
            {
                var name = attrs[j].Name.ToString();
                if (string.Equals(name, "ConventionOverride", StringComparison.Ordinal) ||
                    string.Equals(name, "ConventionOverrideAttribute", StringComparison.Ordinal))
                {
                    return (i, j);
                }
            }
        }

        return null;
    }

    private static bool HasUsing(CompilationUnitSyntax compilationUnit, string namespaceName)
    {
        return compilationUnit.Usings.Any(u =>
            string.Equals(u.Name?.ToString(), namespaceName, StringComparison.Ordinal));
    }
}
