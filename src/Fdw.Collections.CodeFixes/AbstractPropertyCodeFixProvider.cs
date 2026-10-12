using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Fdw.Collections.CodeFixes;

/// <summary>
/// Code fix provider for converting abstract properties to constructor parameters.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AbstractPropertyCodeFixProvider)), Shared]
public class AbstractPropertyCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert abstract property to constructor parameter and update derived classes";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("TC006");

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

        // Find the property declaration
        var property = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        if (property == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedSolution: c => ConvertAbstractPropertyToConstructorParameter(context.Document, property, c),
                equivalenceKey: Title),
            diagnostic);
    }

    // Roslyn document transformation with Syntax API — sequential syntax node updates
#pragma warning disable FDW006, FDW007
    private static async Task<Solution> ConvertAbstractPropertyToConstructorParameter(
        Document document,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return solution;

        // Get the class declaration
        var classDeclaration = property.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration == null) return solution;

        // Get the property symbol
        var propertySymbol = semanticModel.GetDeclaredSymbol(property, cancellationToken);
        if (propertySymbol == null) return solution;

        var propertyName = property.Identifier.Text;
        var propertyType = property.Type;

        // Create parameter name (lowercase first letter)
        var parameterName = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);

        // Find all derived types
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation == null) return solution;

        var baseTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
        if (baseTypeSymbol == null) return solution;

        var derivedTypes = await SymbolFinder.FindDerivedClassesAsync(baseTypeSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        // First, update the base class
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Remove abstract modifier and add protected set
        var newProperty = property
            .WithModifiers(SyntaxFactory.TokenList(property.Modifiers.Where(m => !m.IsKind(SyntaxKind.AbstractKeyword))))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
            {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            })));

        editor.ReplaceNode(property, newProperty);

        // Update base class constructors
        var constructors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().ToList();

        if (constructors.Count == 0)
        {
            // Create a new protected constructor
            var constructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                        .WithType(propertyType))))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(propertyName),
                            SyntaxFactory.IdentifierName(parameterName)))));

            var propertyIndex = classDeclaration.Members.IndexOf(property);
            editor.InsertMembers(classDeclaration, propertyIndex + 1, new[] { constructor });
        }
        else
        {
            // Update existing constructors
            foreach (var constructor in constructors)
            {
                // Add parameter to constructor
                var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                    .WithType(propertyType);

                var newParameterList = constructor.ParameterList.AddParameters(newParameter);

                // Add assignment to constructor body
                var assignment = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(propertyName),
                        SyntaxFactory.IdentifierName(parameterName)));

                ConstructorDeclarationSyntax newConstructor;
                if (constructor.Body != null)
                {
                    // Add assignment at the beginning of the body
                    var newBody = constructor.Body.WithStatements(
                        SyntaxFactory.List(new[] { assignment }.Concat(constructor.Body.Statements)));
                    newConstructor = constructor
                        .WithParameterList(newParameterList)
                        .WithBody(newBody);
                }
                else if (constructor.ExpressionBody != null)
                {
                    // Convert expression body to block body with assignment
                    var existingExpression = SyntaxFactory.ExpressionStatement(constructor.ExpressionBody.Expression);
                    var newBody = SyntaxFactory.Block(assignment, existingExpression);
                    newConstructor = constructor
                        .WithParameterList(newParameterList)
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithBody(newBody);
                }
                else
                {
                    newConstructor = constructor.WithParameterList(newParameterList);
                }

                editor.ReplaceNode(constructor, newConstructor);
            }
        }

        solution = solution.WithDocumentSyntaxRoot(document.Id, editor.GetChangedRoot());

        // Now update all derived classes
        foreach (var derivedType in derivedTypes)
        {
            foreach (var location in derivedType.Locations)
            {
                if (location.IsInSource)
                {
                    var derivedDocument = solution.GetDocument(location.SourceTree);
                    if (derivedDocument != null)
                    {
                        solution = await UpdateDerivedClass(solution, derivedDocument, derivedType, propertySymbol, parameterName, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        return solution;
    }
#pragma warning restore FDW006, FDW007

    // Roslyn document transformation with Syntax API — sequential syntax node updates
#pragma warning disable FDW006, FDW007
    private static async Task<Solution> UpdateDerivedClass(
        Solution solution,
        Document document,
        INamedTypeSymbol derivedType,
        IPropertySymbol propertySymbol,
        string parameterName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return solution;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return solution;

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => semanticModel.GetDeclaredSymbol(c, cancellationToken)?.Equals(derivedType, SymbolEqualityComparer.Default) == true);

        if (classDeclaration == null) return solution;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Find the overridden property
        PropertyDeclarationSyntax? overriddenProperty = null;
        ExpressionSyntax? propertyValue = null;

        foreach (var member in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            var memberSymbol = semanticModel.GetDeclaredSymbol(member, cancellationToken);
            if (memberSymbol?.OverriddenProperty?.Equals(propertySymbol, SymbolEqualityComparer.Default) == true)
            {
                overriddenProperty = member;

                // Extract the value from the property
                if (member.ExpressionBody != null)
                {
                    propertyValue = member.ExpressionBody.Expression;
                }
                else if (member.AccessorList != null)
                {
                    var getter = member.AccessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
                    if (getter?.ExpressionBody != null)
                    {
                        propertyValue = getter.ExpressionBody.Expression;
                    }
                    else if (getter?.Body != null)
                    {
                        var returnStatement = getter.Body.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                        if (returnStatement != null)
                        {
                            propertyValue = returnStatement.Expression;
                        }
                    }
                }
                break;
            }
        }

        // Remove the overridden property
        if (overriddenProperty != null)
        {
            editor.RemoveNode(overriddenProperty);
        }

        // If we couldn't find a value, use a default
        if (propertyValue == null)
        {
            var propertyType = propertySymbol.Type;
#pragma warning disable FDW019 // External Roslyn SpecialType enum — cannot convert to TypeCollection
            if (propertyType.SpecialType == SpecialType.System_String)
            {
                propertyValue = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(string.Empty));
            }
            else if (propertyType.SpecialType == SpecialType.System_Boolean)
            {
                propertyValue = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            }
            else if (propertyType.SpecialType == SpecialType.System_Int32)
            {
                propertyValue = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
            }
            else
            {
                propertyValue = SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(propertyType.ToDisplayString()));
            }
#pragma warning restore FDW019
        }

        // Update constructors to pass the value to base
        var constructors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().ToList();

        foreach (var constructor in constructors)
        {
            ConstructorDeclarationSyntax newConstructor;

            if (constructor.Initializer?.Kind() == SyntaxKind.BaseConstructorInitializer)
            {
                // Add argument to existing base call
                var newInitializer = constructor.Initializer.WithArgumentList(
                    constructor.Initializer.ArgumentList.AddArguments(
                        SyntaxFactory.Argument(propertyValue)));
                newConstructor = constructor.WithInitializer(newInitializer);
            }
            else if (constructor.Initializer?.Kind() == SyntaxKind.ThisConstructorInitializer)
            {
                // Leave this() constructors alone - they'll chain to the one that calls base
                continue;
            }
            else
            {
                // Add base call with the property value
                var newInitializer = SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(propertyValue))));
                newConstructor = constructor.WithInitializer(newInitializer);
            }

            editor.ReplaceNode(constructor, newConstructor);
        }

        // Handle primary constructors (C# 12+)
        if (classDeclaration.ParameterList != null && classDeclaration.BaseList != null)
        {
            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                if (baseType is PrimaryConstructorBaseTypeSyntax primaryBase)
                {
                    // Add argument to primary constructor base call
                    var newArgList = primaryBase.ArgumentList.AddArguments(
                        SyntaxFactory.Argument(propertyValue));
                    var newPrimaryBase = primaryBase.WithArgumentList(newArgList);
                    editor.ReplaceNode(primaryBase, newPrimaryBase);
                    break;
                }
            }
        }

        return solution.WithDocumentSyntaxRoot(document.Id, editor.GetChangedRoot());
    }
#pragma warning restore FDW006, FDW007
}