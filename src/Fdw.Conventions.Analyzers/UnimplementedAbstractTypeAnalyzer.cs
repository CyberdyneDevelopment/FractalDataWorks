using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that reports interfaces and abstract classes defined in source that have no
/// concrete implementation within the current compilation. Useful for identifying dead
/// abstractions that were never implemented or whose implementations were removed.
/// </summary>
/// <remarks>
/// Enabled by default at Info severity. Override via .editorconfig:
/// <code>dotnet_diagnostic.FDW020.severity = warning</code>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnimplementedAbstractTypeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for unimplemented abstract type.
    /// </summary>
    public const string DiagnosticId = "FDW020";

    private const string Title = "Abstract type has no implementation";
    private const string MessageFormat = "{0} '{1}' has no implementation in the current compilation";
    private const string Description =
        "Interfaces and abstract classes with no concrete implementation may indicate dead code. " +
        "If the type is intended for external implementation, suppress this diagnostic.";
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
            // Collect all source-defined interfaces and abstract classes
            var abstractTypes = new ConcurrentBag<INamedTypeSymbol>();

            // Collect all base types and implemented interfaces from concrete types
            var implementedTypes = new ConcurrentBag<INamedTypeSymbol>();

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                // Only source-defined types
                if (typeSymbol.Locations.Length == 0 || !typeSymbol.Locations[0].IsInSource)
                    return;

                // Skip compiler-generated types
                if (typeSymbol.IsImplicitlyDeclared)
                    return;

                // Skip nested types
                if (typeSymbol.ContainingType != null)
                    return;

                // Skip types with suppression attributes
                if (HasGeneratedCodeAttribute(typeSymbol))
                    return;

                if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    // Skip marker interfaces (no members at all) — they serve a tagging purpose
                    if (!typeSymbol.GetMembers().Any(m => !m.IsImplicitlyDeclared))
                        return;

                    abstractTypes.Add(typeSymbol);
                }
                else if (typeSymbol.IsAbstract && typeSymbol.TypeKind == TypeKind.Class)
                {
                    abstractTypes.Add(typeSymbol);
                }

                // For non-abstract types, record all their base types and interfaces
                if (!typeSymbol.IsAbstract && typeSymbol.TypeKind == TypeKind.Class)
                {
                    CollectImplementedTypes(typeSymbol, implementedTypes);
                }
                else if (typeSymbol.TypeKind == TypeKind.Struct)
                {
                    CollectImplementedTypes(typeSymbol, implementedTypes);
                }
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                // Build a set of all types that have at least one implementation
                var implementedSet = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                foreach (var t in implementedTypes)
                {
                    implementedSet.Add(t);
                }

                // Deduplicate abstract types (partial classes may produce duplicates)
                var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                foreach (var abstractType in abstractTypes)
                {
                    if (!seen.Add(abstractType))
                        continue;

                    // Check if any concrete type implements/extends this abstract type
                    if (implementedSet.Contains(abstractType))
                        continue;

                    // For generic types, also check if the unbound form is implemented
                    if (abstractType.IsGenericType)
                    {
                        var originalDef = abstractType.OriginalDefinition;
                        if (implementedSet.Contains(originalDef))
                            continue;
                    }

                    var kind = abstractType.TypeKind == TypeKind.Interface ? "Interface" : "Abstract class";

                    foreach (var location in abstractType.Locations)
                    {
                        if (location.IsInSource)
                        {
                            var diagnostic = Diagnostic.Create(
                                Rule,
                                location,
                                kind,
                                abstractType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

                            endContext.ReportDiagnostic(diagnostic);
                            break;
                        }
                    }
                }
            });
        });
    }

    private static void CollectImplementedTypes(
        INamedTypeSymbol typeSymbol,
        ConcurrentBag<INamedTypeSymbol> implementedTypes)
    {
        // Walk the base type chain
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Locations.Length > 0 && baseType.Locations[0].IsInSource)
            {
                implementedTypes.Add(baseType.OriginalDefinition);
            }

            baseType = baseType.BaseType;
        }

        // Collect all implemented interfaces (including inherited)
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.Locations.Length > 0 && iface.Locations[0].IsInSource)
            {
                implementedTypes.Add(iface.OriginalDefinition);
            }
        }
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
            {
                return true;
            }
        }

        return false;
    }
}
