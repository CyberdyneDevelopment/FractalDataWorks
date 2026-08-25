using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that reports source-defined types that are never referenced anywhere in the
/// current compilation. Helps identify dead types that can be safely removed.
/// </summary>
/// <remarks>
/// Disabled by default because public types may be used by other assemblies, via reflection,
/// or through dependency injection. Enable via .editorconfig:
/// <code>dotnet_diagnostic.FDW021.severity = suggestion</code>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedTypeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for unused type.
    /// </summary>
    public const string DiagnosticId = "FDW021";

    private const string Title = "Type is not referenced in the compilation";
    private const string MessageFormat = "Type '{0}' is not referenced anywhere in the current compilation";
    private const string Description =
        "Types that are never referenced may be dead code that can be removed. " +
        "If the type is used externally, via reflection, or through DI registration, suppress this diagnostic.";
    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // All source-defined types (keyed by original definition for generics)
            var sourceTypes = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);

            // All types that are referenced somewhere
            var referencedTypes = new ConcurrentBag<INamedTypeSymbol>();

            // Phase 1: Collect all source-defined types
            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                // Only source-defined types
                if (typeSymbol.Locations.Length == 0 || !typeSymbol.Locations[0].IsInSource)
                    return;

                // Skip compiler-generated types
                if (typeSymbol.IsImplicitlyDeclared)
                    return;

                // Skip nested types — tracked via their containing type
                if (typeSymbol.ContainingType != null)
                    return;

                // Skip types with generated code attributes
                if (HasGeneratedCodeAttribute(typeSymbol))
                    return;

                // Skip entry points — these are never "referenced" but are required
                if (IsEntryPointType(typeSymbol))
                    return;

                // Skip types decorated with well-known attributes that imply external usage
                if (HasExternalUsageAttribute(typeSymbol))
                    return;

                sourceTypes.TryAdd(typeSymbol.OriginalDefinition, 0);
            }, SymbolKind.NamedType);

            // Phase 2: Walk syntax trees to find all type references
            compilationContext.RegisterSyntaxNodeAction(nodeContext =>
            {
                var semanticModel = nodeContext.SemanticModel;
                CollectReferencedType(nodeContext.Node, semanticModel, referencedTypes);
            },
            SyntaxKind.IdentifierName,
            SyntaxKind.GenericName,
            SyntaxKind.QualifiedName,
            SyntaxKind.SimpleBaseType,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.TypeOfExpression,
            SyntaxKind.IsExpression,
            SyntaxKind.IsPatternExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.CastExpression,
            SyntaxKind.InvocationExpression,
            SyntaxKind.Attribute);

            // Phase 3: Report unreferenced types
            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                var referencedSet = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                foreach (var t in referencedTypes)
                {
                    referencedSet.Add(t.OriginalDefinition);
                }

                foreach (var kvp in sourceTypes)
                {
                    var typeSymbol = kvp.Key;

                    // Skip if referenced
                    if (referencedSet.Contains(typeSymbol))
                        continue;

                    // Self-references don't count, but base type references do —
                    // a type that only references itself is still unused
                    // However, if the type is a base class with derived types, it's referenced
                    // (handled by the base type syntax node collection)

                    foreach (var location in typeSymbol.Locations)
                    {
                        if (location.IsInSource)
                        {
                            var diagnostic = Diagnostic.Create(
                                Rule,
                                location,
                                typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

                            endContext.ReportDiagnostic(diagnostic);
                            break;
                        }
                    }
                }
            });
        });
    }

    private static void CollectReferencedType(
        SyntaxNode node,
        SemanticModel semanticModel,
        ConcurrentBag<INamedTypeSymbol> referencedTypes)
    {
        ISymbol? symbol = null;

        switch (node)
        {
            case BaseTypeSyntax baseType:
                symbol = semanticModel.GetSymbolInfo(baseType.Type).Symbol;
                break;

            case TypeOfExpressionSyntax typeOf:
                symbol = semanticModel.GetSymbolInfo(typeOf.Type).Symbol;
                break;

            case ObjectCreationExpressionSyntax creation:
                symbol = semanticModel.GetSymbolInfo(creation).Symbol?.ContainingType;
                break;

            case ImplicitObjectCreationExpressionSyntax implicitCreation:
                var typeInfo = semanticModel.GetTypeInfo(implicitCreation);
                symbol = typeInfo.Type;
                break;

            case CastExpressionSyntax cast:
                symbol = semanticModel.GetSymbolInfo(cast.Type).Symbol;
                break;

            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.IsExpression)
                                                    || binary.IsKind(SyntaxKind.AsExpression):
                symbol = semanticModel.GetSymbolInfo(binary.Right).Symbol;
                break;

            case IsPatternExpressionSyntax isPattern:
                CollectPatternTypes(isPattern.Pattern, semanticModel, referencedTypes);
                return;

            case InvocationExpressionSyntax invocation:
                var invokedSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
                if (invokedSymbol is IMethodSymbol method)
                {
                    // Collect type arguments for generic method calls
                    foreach (var typeArg in method.TypeArguments)
                    {
                        if (typeArg is INamedTypeSymbol namedArg)
                            referencedTypes.Add(namedArg);
                    }

                    // The containing type of the method
                    if (method.ContainingType != null)
                        referencedTypes.Add(method.ContainingType);

                    // Extension method receiver type
                    if (method.IsExtensionMethod && method.ReceiverType is INamedTypeSymbol receiverType)
                        referencedTypes.Add(receiverType);
                }

                return;

            case AttributeSyntax:
                symbol = semanticModel.GetSymbolInfo(node).Symbol?.ContainingType;
                break;

            case IdentifierNameSyntax:
            case GenericNameSyntax:
            case QualifiedNameSyntax:
                // Skip if this is part of a namespace declaration — not a type reference
                if (node.Parent is NamespaceDeclarationSyntax
                    || node.Parent is FileScopedNamespaceDeclarationSyntax
                    || node.Parent is UsingDirectiveSyntax)
                    return;

                symbol = semanticModel.GetSymbolInfo(node).Symbol;
                break;
        }

        if (symbol is INamedTypeSymbol namedType)
        {
            referencedTypes.Add(namedType);

            // Also add type arguments for constructed generic types
            if (namedType.IsGenericType)
            {
                foreach (var typeArg in namedType.TypeArguments)
                {
                    if (typeArg is INamedTypeSymbol argType)
                        referencedTypes.Add(argType);
                }
            }
        }
        else if (symbol is ITypeSymbol typeSymbol && typeSymbol is INamedTypeSymbol named)
        {
            referencedTypes.Add(named);
        }
    }

    private static void CollectPatternTypes(
        PatternSyntax pattern,
        SemanticModel semanticModel,
        ConcurrentBag<INamedTypeSymbol> referencedTypes)
    {
        switch (pattern)
        {
            case DeclarationPatternSyntax declaration:
                if (semanticModel.GetSymbolInfo(declaration.Type).Symbol is INamedTypeSymbol declType)
                    referencedTypes.Add(declType);
                break;

            case RecursivePatternSyntax recursive when recursive.Type != null:
                if (semanticModel.GetSymbolInfo(recursive.Type).Symbol is INamedTypeSymbol recType)
                    referencedTypes.Add(recType);
                break;

            case BinaryPatternSyntax binary:
                CollectPatternTypes(binary.Left, semanticModel, referencedTypes);
                CollectPatternTypes(binary.Right, semanticModel, referencedTypes);
                break;

            case UnaryPatternSyntax unary:
                CollectPatternTypes(unary.Pattern, semanticModel, referencedTypes);
                break;

            case ParenthesizedPatternSyntax paren:
                CollectPatternTypes(paren.Pattern, semanticModel, referencedTypes);
                break;
        }
    }

    private static bool IsEntryPointType(INamedTypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;

        // Program class (top-level statements generate a Program class)
        if (string.Equals(name, "Program", StringComparison.Ordinal))
            return true;

        // Startup class (ASP.NET convention)
        if (string.Equals(name, "Startup", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool HasExternalUsageAttribute(INamedTypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == null)
                continue;

            // TypeCollection/ServiceType attributes — these types are discovered by generators
            if (attrName.StartsWith("TypeOption", StringComparison.Ordinal)
                || attrName.StartsWith("ServiceTypeOption", StringComparison.Ordinal)
                || attrName.StartsWith("TypeCollection", StringComparison.Ordinal)
                || attrName.StartsWith("ServiceTypeCollection", StringComparison.Ordinal)
                || attrName.StartsWith("MutableTypeCollection", StringComparison.Ordinal)
                || attrName.StartsWith("MutableServiceTypeCollection", StringComparison.Ordinal)
                || attrName.StartsWith("EnumOption", StringComparison.Ordinal))
                return true;

            // MessageLogging — static partial classes referenced by generated code
            if (string.Equals(attrName, "MessageLoggingAttribute", StringComparison.Ordinal))
                return true;

            // ManagedConfiguration — discovered by configuration infrastructure
            if (string.Equals(attrName, "ManagedConfigurationAttribute", StringComparison.Ordinal))
                return true;

            // GenerateMapper — used by data access layer
            if (string.Equals(attrName, "GenerateMapperAttribute", StringComparison.Ordinal))
                return true;

            // xUnit test classes
            if (string.Equals(attrName, "CollectionAttribute", StringComparison.Ordinal)
                || string.Equals(attrName, "CollectionDefinitionAttribute", StringComparison.Ordinal))
                return true;

            // ASP.NET / FastEndpoints — discovered by framework
            if (string.Equals(attrName, "ApiControllerAttribute", StringComparison.Ordinal))
                return true;
        }

        // Check if it's a test class (has methods with [Fact] or [Theory])
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                foreach (var methodAttr in method.GetAttributes())
                {
                    var methodAttrName = methodAttr.AttributeClass?.Name;
                    if (string.Equals(methodAttrName, "FactAttribute", StringComparison.Ordinal)
                        || string.Equals(methodAttrName, "TheoryAttribute", StringComparison.Ordinal)
                        || string.Equals(methodAttrName, "TestAttribute", StringComparison.Ordinal))
                        return true;
                }
            }
        }

        // Check if it's a FastEndpoint (inherits from Endpoint base classes)
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            var baseName = baseType.Name;
            if (string.Equals(baseName, "Endpoint", StringComparison.Ordinal)
                || string.Equals(baseName, "EndpointWithoutRequest", StringComparison.Ordinal)
                || string.Equals(baseName, "EndpointBase", StringComparison.Ordinal))
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool HasGeneratedCodeAttribute(INamedTypeSymbol typeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == null)
                continue;

            if (string.Equals(name, "GeneratedCodeAttribute", StringComparison.Ordinal)
                || string.Equals(name, "CompilerGeneratedAttribute", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
