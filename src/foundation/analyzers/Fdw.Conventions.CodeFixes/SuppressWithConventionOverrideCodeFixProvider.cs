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
/// <remarks>
/// It writes exactly the value the analyzer measured, which it reads from the diagnostic's
/// <see cref="ActualValueProperty"/>. With no measured value it offers no fix: an invented number is an assumed
/// value, against the no-fallbacks rule (FDW-3504).
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SuppressWithConventionOverrideCodeFixProvider)), Shared]
public class SuppressWithConventionOverrideCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// The diagnostic property that carries the measured value. ConventionOverrideHelper.ActualValueProperty in
    /// Fdw.Conventions.Analyzers sets it under the same name; this project does not reference that one.
    /// </summary>
    private const string ActualValueProperty = "ActualValue";

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

        // The measured unit is the innermost method, constructor, destructor, local function or lambda at the
        // diagnostic: FDW006 measures a lambda or local function apart from the method that declares it.
        var target = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?
            .AncestorsAndSelf()
            .FirstOrDefault(node => node is BaseMethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or AnonymousFunctionExpressionSyntax);

        // A simple lambda (x => ...) and an anonymous method cannot carry an attribute without being rewritten.
        if (target is null || !CanCarryAttribute(target))
            return;

        if (!diagnostic.Properties.TryGetValue(ActualValueProperty, out var measured) ||
            !int.TryParse(measured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var measuredValue))
            return;

        var propertyName = string.Equals(diagnostic.Id, "FDW006", StringComparison.Ordinal)
            ? "MaxMethodLines"
            : "MaxCyclomaticComplexity";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: string.Format(
                    CultureInfo.InvariantCulture,
                    "Add [ConventionOverride({0} = {1})]",
                    propertyName,
                    measuredValue),
                createChangedDocument: c => AddConventionOverrideAttribute(
                    context.Document, target, propertyName, measuredValue, c),
                equivalenceKey: $"ConventionOverride_{diagnostic.Id}"),
            diagnostic);
    }

    private static bool CanCarryAttribute(SyntaxNode target)
        => target is BaseMethodDeclarationSyntax
            or LocalFunctionStatementSyntax
            or ParenthesizedLambdaExpressionSyntax;

    private static SyntaxList<AttributeListSyntax> GetAttributeLists(SyntaxNode target) => target switch
    {
        BaseMethodDeclarationSyntax method => method.AttributeLists,
        LocalFunctionStatementSyntax localFunction => localFunction.AttributeLists,
        ParenthesizedLambdaExpressionSyntax lambda => lambda.AttributeLists,
        _ => throw new InvalidOperationException($"A {target.Kind()} cannot carry [ConventionOverride]."),
    };

    private static SyntaxNode WithAttributeLists(SyntaxNode target, SyntaxList<AttributeListSyntax> attributeLists) => target switch
    {
        BaseMethodDeclarationSyntax method => method.WithAttributeLists(attributeLists),
        LocalFunctionStatementSyntax localFunction => localFunction.WithAttributeLists(attributeLists),
        ParenthesizedLambdaExpressionSyntax lambda => lambda.WithAttributeLists(attributeLists),
        _ => throw new InvalidOperationException($"A {target.Kind()} cannot carry [ConventionOverride]."),
    };

    private static async Task<Document> AddConventionOverrideAttribute(
        Document document,
        SyntaxNode target,
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
                SyntaxFactory.Literal(value)))
            .NormalizeWhitespace();

        var existingAttrLists = GetAttributeLists(target);
        SyntaxNode newTarget;

        var existingOverride = FindExistingConventionOverride(existingAttrLists);
        if (existingOverride.HasValue)
        {
            // Merge into the existing [ConventionOverride(...)]: replace the property's value when it is already named,
            // since a second named argument for one property does not compile.
            var (listIndex, attrIndex) = existingOverride.Value;
            var existingAttr = existingAttrLists[listIndex].Attributes[attrIndex];
            var existingArgList = existingAttr.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
            var existingArgument = existingArgList.Arguments.FirstOrDefault(argument =>
                argument.NameEquals != null &&
                string.Equals(argument.NameEquals.Name.Identifier.Text, propertyName, StringComparison.Ordinal));
            var arguments = existingArgList.Arguments;
            var newArgList = existingArgument != null
                ? existingArgList.ReplaceNode(existingArgument, attributeArgument.WithTriviaFrom(existingArgument))
                : existingArgList.WithArguments(SyntaxFactory.SeparatedList(
                    arguments.Append(attributeArgument),
                    arguments.Count == 0
                        ? Enumerable.Empty<SyntaxToken>()
                        : arguments.GetSeparators().Append(
                            SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space))));
            var newAttr = existingAttr.WithArgumentList(newArgList);

            var updatedAttrList = existingAttrLists[listIndex].WithAttributes(
                existingAttrLists[listIndex].Attributes.Replace(existingAttr, newAttr));

            newTarget = WithAttributeLists(target, existingAttrLists.Replace(existingAttrLists[listIndex], updatedAttrList));
        }
        else
        {
            // Add new [ConventionOverride(PropertyName = value)] attribute
            var attribute = SyntaxFactory.Attribute(
                SyntaxFactory.ParseName("ConventionOverride"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(attributeArgument)));

            newTarget = AddAttributeList(target, SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));
        }

        var newRoot = root.ReplaceNode(target, newTarget);

        // Add using directive if needed
        var compilationUnit = (CompilationUnitSyntax)newRoot;
        if (!HasUsing(compilationUnit, "Fdw.Conventions"))
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("Fdw.Conventions"))
                .WithTrailingTrivia(FirstEndOfLine(compilationUnit) is { } endOfLine ? endOfLine : SyntaxFactory.Space);
            newRoot = compilationUnit.AddUsings(usingDirective);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Adds <paramref name="attributeList"/> after the target's existing attribute lists, writing its trivia by hand.
    /// </summary>
    /// <remarks>
    /// Why no formatter: formatting the changed node rewrote every line ending inside it to the machine's default,
    /// so a fix touched lines it had no business with. A declaration's new attribute takes the declaration's leading
    /// trivia (or, after existing attributes, its indentation) and ends with the line ending the declaration already
    /// uses; a lambda, or a declaration written on one line, gets a single space.
    /// </remarks>
    private static SyntaxNode AddAttributeList(SyntaxNode target, AttributeListSyntax attributeList)
    {
        var existing = GetAttributeLists(target);
        var endOfLine = target is ParenthesizedLambdaExpressionSyntax ? null : FirstEndOfLine(target);

        if (endOfLine is null)
        {
            var sameLine = existing.Count == 0
                ? attributeList.WithLeadingTrivia(target.GetLeadingTrivia()).WithTrailingTrivia(SyntaxFactory.Space)
                : attributeList.WithTrailingTrivia(SyntaxFactory.Space);
            var bare = existing.Count == 0 ? target.WithoutLeadingTrivia() : target;
            return WithAttributeLists(bare, GetAttributeLists(bare).Add(sameLine));
        }

        var leading = target.GetLeadingTrivia();
        var indentation = Indentation(leading);
        if (existing.Count == 0)
        {
            return WithAttributeLists(
                target.WithLeadingTrivia(indentation),
                SyntaxFactory.SingletonList(attributeList.WithLeadingTrivia(leading).WithTrailingTrivia(endOfLine.Value)));
        }

        return WithAttributeLists(
            target,
            existing.Add(attributeList.WithLeadingTrivia(indentation).WithTrailingTrivia(endOfLine.Value)));
    }

    /// <summary>The whitespace after the last line break in <paramref name="leading"/>: the declaration's indentation.</summary>
    private static SyntaxTriviaList Indentation(SyntaxTriviaList leading)
    {
        var lastLineBreak = -1;
        for (var i = 0; i < leading.Count; i++)
        {
            if (leading[i].IsKind(SyntaxKind.EndOfLineTrivia) || leading[i].IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                lastLineBreak = i;
        }

        return SyntaxFactory.TriviaList(
            leading.Skip(lastLineBreak + 1).TakeWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)));
    }

    /// <summary>The first line break inside <paramref name="node"/>, or null when it is written on one line.</summary>
    private static SyntaxTrivia? FirstEndOfLine(SyntaxNode node)
    {
        foreach (var trivia in node.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return trivia;
        }

        return null;
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
