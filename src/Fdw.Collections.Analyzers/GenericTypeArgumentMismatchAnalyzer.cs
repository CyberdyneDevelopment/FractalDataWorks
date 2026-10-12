using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Collections.Analyzers;

/// <summary>
/// Analyzer that detects when a [TypeOption] attribute references a closed generic collection type
/// but the option type inherits from a base class with incompatible generic type arguments.
/// Example: [TypeOption(typeof(GenericTypes&lt;string&gt;))] on a class inheriting GenericBase&lt;int&gt;
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GenericTypeArgumentMismatchAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for generic type argument mismatch between TypeOption and base class.
    /// </summary>
    public const string DiagnosticId = "TC004";

    private static readonly LocalizableString Title = "Generic type argument mismatch between TypeOption attribute and base class";
    private static readonly LocalizableString MessageFormat = "Type '{0}' has [TypeOption(typeof({1}<{2}>))] but inherits from {3}<{4}>. Generic type arguments must match: {2} != {4}.";
    private static readonly LocalizableString Description = "When a TypeOption attribute references a closed generic collection type, the option type must inherit from a base class with the same generic type argument.";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    /// <summary>
    /// Gets the supported diagnostic descriptors for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <summary>
    /// Initializes the analyzer by registering symbol analysis actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Skip abstract types, interfaces, and static classes
        if (namedType.IsAbstract || namedType.TypeKind == TypeKind.Interface || namedType.IsStatic)
            return;

        // Get TypeOption attribute type
        var typeOptionAttribute = context.Compilation.GetTypeByMetadataName("Fdw.Collections.Attributes.TypeOptionAttribute");
        if (typeOptionAttribute == null)
            return;

        // Find [TypeOption] attributes on this type
        var typeOptionAttrs = namedType.GetAttributes()
            .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, typeOptionAttribute))
            .ToList();

        if (typeOptionAttrs.Count == 0)
            return;

        // Check each [TypeOption] attribute
        foreach (var attr in typeOptionAttrs)
        {
            AnalyzeTypeOptionAttribute(context, namedType, attr);
        }
    }

    private static void AnalyzeTypeOptionAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol optionType,
        AttributeData typeOptionAttr)
    {
        // Extract collection type from [TypeOption(typeof(CollectionType))]
        if (typeOptionAttr.ConstructorArguments.Length == 0)
            return;

        var collectionTypeArg = typeOptionAttr.ConstructorArguments[0];
        if (collectionTypeArg.Kind != TypedConstantKind.Type || collectionTypeArg.Value is not INamedTypeSymbol collectionType)
            return;

        // Check if collection type is a closed generic (e.g., GenericTypes<string>)
        if (!collectionType.IsGenericType || collectionType.IsUnboundGenericType)
            return; // Not a closed generic, nothing to validate

        // Extract generic type arguments from collection (e.g., <string> from GenericTypes<string>)
        var collectionGenericArgs = collectionType.TypeArguments;
        if (collectionGenericArgs.Length == 0)
            return;

        // Get the TypeCollection attribute on the collection class to find the base type
        var typeCollectionAttribute = context.Compilation.GetTypeByMetadataName("Fdw.Collections.Attributes.TypeCollectionAttribute");
        if (typeCollectionAttribute == null)
            return;

        // Find TypeCollection attribute on the collection type
        var collectionDefAttr = collectionType.OriginalDefinition.GetAttributes()
            .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, typeCollectionAttribute));

        if (collectionDefAttr == null || collectionDefAttr.ConstructorArguments.Length == 0)
            return;

        // Extract base type from TypeCollection attribute (e.g., GenericBase<>)
        var baseTypeArg = collectionDefAttr.ConstructorArguments[0];
        if (baseTypeArg.Kind != TypedConstantKind.Type || baseTypeArg.Value is not INamedTypeSymbol expectedBaseType)
            return;

        // Check if expected base type is generic
        if (!expectedBaseType.IsGenericType)
            return; // Base type is not generic, no argument matching needed

        // Now find what the option type actually inherits from
        var actualBaseType = FindGenericBaseType(optionType, expectedBaseType);
        if (actualBaseType == null)
            return; // Option type doesn't inherit from the expected generic base family

        // Compare generic type arguments
        var expectedGenericArg = collectionGenericArgs[0]; // e.g., "string" from GenericTypes<string>
        var actualGenericArg = actualBaseType.TypeArguments.FirstOrDefault(); // e.g., "int" from GenericBase<int>

        if (actualGenericArg == null)
            return;

        // Check if they match
        if (!SymbolEqualityComparer.Default.Equals(expectedGenericArg, actualGenericArg))
        {
            // MISMATCH DETECTED!
            ReportMismatch(context, optionType, collectionType, expectedGenericArg, actualBaseType, actualGenericArg);
        }
    }

    /// <summary>
    /// Finds a base class in the inheritance chain that matches the expected generic type definition.
    /// E.g., finds GenericBase&lt;int&gt; when looking for GenericBase&lt;&gt; in the inheritance chain.
    /// </summary>
    private static INamedTypeSymbol? FindGenericBaseType(INamedTypeSymbol type, INamedTypeSymbol expectedGenericDefinition)
    {
        var current = type.BaseType;
        var expectedDefinition = expectedGenericDefinition.IsGenericType
            ? expectedGenericDefinition.ConstructedFrom
            : expectedGenericDefinition;

        while (current != null)
        {
            if (current.IsGenericType)
            {
                var currentDefinition = current.ConstructedFrom;
                if (SymbolEqualityComparer.Default.Equals(currentDefinition, expectedDefinition))
                {
                    return current; // Found matching generic base type
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Reports the generic type argument mismatch diagnostic.
    /// </summary>
    private static void ReportMismatch(
        SymbolAnalysisContext context,
        INamedTypeSymbol optionType,
        INamedTypeSymbol collectionType,
        ITypeSymbol expectedGenericArg,
        INamedTypeSymbol actualBaseType,
        ITypeSymbol actualGenericArg)
    {
        // Find the TypeOption attribute syntax for precise location
        var syntaxReferences = optionType.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0)
            return;

        var syntaxNode = syntaxReferences[0].GetSyntax();
        if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            return;

        // Try to find the TypeOption attribute syntax
        var attributeLocation = classDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString().Contains("TypeOption"))
            ?.GetLocation() ?? classDeclaration.Identifier.GetLocation();

        var collectionName = collectionType.OriginalDefinition.Name;
        var expectedArg = expectedGenericArg.ToDisplayString();
        var actualBaseName = actualBaseType.OriginalDefinition.Name;
        var actualArg = actualGenericArg.ToDisplayString();

        var diagnostic = Diagnostic.Create(
            Rule,
            attributeLocation,
            optionType.Name,
            collectionName,
            expectedArg,
            actualBaseName,
            actualArg);

        context.ReportDiagnostic(diagnostic);
    }
}
