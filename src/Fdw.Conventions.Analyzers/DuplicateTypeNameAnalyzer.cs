using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that warns when multiple types in the same compilation share the same simple name
/// (in different namespaces). This causes ambiguous references requiring fully-qualified names or aliases.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateTypeNameAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for duplicate type name violation.
    /// </summary>
    public const string DiagnosticId = "FDW009";

    private const string Title = "Duplicate type name in compilation";
    private const string MessageFormat = "Type '{0}' in namespace '{1}' has the same name as type in namespace '{2}'";
    private const string Description = "Multiple types with the same name in different namespaces cause ambiguous references. Consider renaming one to be more specific.";
    private const string Category = "Naming";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
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
            var typesByName = new ConcurrentDictionary<string, ConcurrentBag<INamedTypeSymbol>>(StringComparer.Ordinal);

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                // Only source-defined types
                if (typeSymbol.Locations.Length == 0 || !typeSymbol.Locations[0].IsInSource)
                    return;

                // Skip nested types (Builder pattern etc. is common)
                if (typeSymbol.ContainingType != null)
                    return;

                // Skip compiler-generated types
                if (typeSymbol.IsImplicitlyDeclared)
                    return;

                // Skip non-public types (internal duplicates are intentional scoping)
                if (typeSymbol.DeclaredAccessibility != Accessibility.Public)
                    return;

                // Skip TypeCollection/ServiceTypeCollection types — these are intentionally
                // short-named (e.g., "None") because they're accessed via their collection
                if (IsTypeCollectionMember(typeSymbol))
                    return;

                // Skip types with empty names
                var name = typeSymbol.Name;
                if (name.Length == 0)
                    return;

                // Include arity in key so None and None<T> are not considered duplicates
                var key = typeSymbol.Arity > 0
                    ? name + "`" + typeSymbol.Arity.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : name;

                var bag = typesByName.GetOrAdd(key, _ => new ConcurrentBag<INamedTypeSymbol>());
                bag.Add(typeSymbol);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (var pair in typesByName)
                {
                    var types = pair.Value.ToArray();
                    if (types.Length < 2)
                        continue;

                    // Deduplicate: partial classes produce one symbol per declaration but
                    // the symbol is the same. Use SymbolEqualityComparer to find truly distinct types.
                    var distinct = new List<INamedTypeSymbol>();
                    foreach (var t in types)
                    {
                        var isDuplicate = false;
                        foreach (var existing in distinct)
                        {
                            if (SymbolEqualityComparer.Default.Equals(t, existing))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (!isDuplicate)
                            distinct.Add(t);
                    }

                    if (distinct.Count < 2)
                        continue;

                    // Report on each type, referencing the first OTHER type's namespace
                    for (var i = 0; i < distinct.Count; i++)
                    {
                        var current = distinct[i];
                        var other = distinct[i == 0 ? 1 : 0];

                        var currentNamespace = current.ContainingNamespace?.ToDisplayString() ?? "<global>";
                        var otherNamespace = other.ContainingNamespace?.ToDisplayString() ?? "<global>";

                        // Same namespace = CS0101 (compiler handles it), skip
                        if (string.Equals(currentNamespace, otherNamespace, StringComparison.Ordinal))
                            continue;

                        foreach (var location in current.Locations)
                        {
                            if (location.IsInSource)
                            {
                                var diagnostic = Diagnostic.Create(
                                    Rule,
                                    location,
                                    current.Name,
                                    currentNamespace,
                                    otherNamespace);

                                endContext.ReportDiagnostic(diagnostic);
                                break; // Only report on first source location per type
                            }
                        }
                    }
                }
            });
        });
    }

    private static bool IsTypeCollectionMember(INamedTypeSymbol typeSymbol)
    {
        // Check for [TypeOption], [ServiceTypeOption], [TypeCollection], or [ServiceTypeCollection] attributes
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == null)
                continue;

            if (attrName.StartsWith("TypeOption", StringComparison.Ordinal)
                || attrName.StartsWith("ServiceTypeOption", StringComparison.Ordinal)
                || attrName.StartsWith("TypeCollection", StringComparison.Ordinal)
                || attrName.StartsWith("ServiceTypeCollection", StringComparison.Ordinal)
                || attrName.StartsWith("ServiceServiceTypeOption", StringComparison.Ordinal)
                || attrName.StartsWith("EnhancedEnumBase", StringComparison.Ordinal)
                || attrName.StartsWith("EnumOption", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
