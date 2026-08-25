using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Collections.Analyzers;

/// <summary>
/// Analyzer that detects duplicate lookup values in enhanced enum collections when AllowMultiple is not enabled.
/// Reports warnings when multiple enum options have the same value for a lookup property without explicit AllowMultiple permission.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DuplicateLookupValueAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for duplicate lookup values in enhanced enum collections.
    /// </summary>
    public const string DiagnosticId = "ENHENUM001";

    private static readonly LocalizableString Title =
        "Duplicate lookup values detected";
    private static readonly LocalizableString MessageFormat =
        "The property '{0}' has duplicate values but AllowMultiple is not set to true. Found duplicate value '{1}' in types: {2}.";
    private static readonly LocalizableString Description =
        "When multiple enum options have the same value for a lookup property, the EnumLookup attribute must have AllowMultiple set to true.";

    private const string Category = "Collections";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        customTags: ["CompilationEnd"]);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var enumCollectionTypes = new List<INamedTypeSymbol>();
            var enumOptionTypes = new List<INamedTypeSymbol>();

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var symbol = (INamedTypeSymbol)symbolContext.Symbol;
                if (HasAttribute(symbol, "EnumCollection"))
                {
                    enumCollectionTypes.Add(symbol);
                }
                if (HasAttribute(symbol, "EnumOption"))
                {
                    enumOptionTypes.Add(symbol);
                }
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (var collectionType in enumCollectionTypes)
                {
                    AnalyzeEnumCollection(endContext, collectionType, enumOptionTypes);
                }
            });
        });
    }


    private static void AnalyzeEnumCollection(
        CompilationAnalysisContext context,
        INamedTypeSymbol collectionType,
        List<INamedTypeSymbol> enumOptionTypes)
    {
        // Find all properties with EnumLookup attribute
        var lookupProperties = new Dictionary<string, (IPropertySymbol Property, bool AllowMultiple)>(StringComparer.Ordinal);

        foreach (var member in collectionType.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                var lookupAttr = GetAttribute(property, "EnumLookup");
                if (lookupAttr != null)
                {
                    var allowMultiple = false;

                    // Check if AllowMultiple is set
                    if (lookupAttr.ConstructorArguments.Length > 1)
                    {
                        var secondArg = lookupAttr.ConstructorArguments[1];
                        if (secondArg.Value is bool b)
                        {
                            allowMultiple = b;
                        }
                    }

                    // Also check named arguments
                    foreach (var namedArg in lookupAttr.NamedArguments)
                    {
                        if (string.Equals(namedArg.Key, "AllowMultiple", StringComparison.Ordinal) && namedArg.Value.Value is bool b)
                        {
                            allowMultiple = b;
                        }
                    }

                    lookupProperties[property.Name] = (property, allowMultiple);
                }
            }
        }

        // Find all derived types (enum options)
        var derivedTypes = enumOptionTypes
            .Where(t => IsDerivedFrom(t, collectionType))
            .ToList();

        // For each lookup property, check for duplicates
        foreach (var kvp in lookupProperties)
        {
            var propName = kvp.Key;
            var property = kvp.Value.Property;
            var allowMultiple = kvp.Value.AllowMultiple;

            if (!allowMultiple)
            {
                CheckForDuplicates(context, property, derivedTypes, propName);
            }
        }
    }

    private static void CheckForDuplicates(
        CompilationAnalysisContext context,
        IPropertySymbol lookupProperty,
        List<INamedTypeSymbol> derivedTypes,
        string propertyName)
    {
        var valueToTypes = new Dictionary<object, List<string>>();

        foreach (var derivedType in derivedTypes)
        {
            // Try to get the property value from the derived type
            var prop = derivedType.GetMembers(propertyName).FirstOrDefault() as IPropertySymbol;
            if (prop != null)
            {
                // Try to get the constant value if it's a simple property
                var value = GetPropertyValue(derivedType, propertyName);
                if (value != null)
                {
                    if (!valueToTypes.TryGetValue(value, out var types))
                    {
                        types = [];
                        valueToTypes[value] = types;
                    }
                    types.Add(derivedType.Name);
                }
            }
        }

        // Report duplicates
        foreach (var kvp in valueToTypes)
        {
            var value = kvp.Key;
            var types = kvp.Value;

            if (types.Count > 1)
            {
                // Find the location of the property declaration
                var location = GetPropertyLocation(lookupProperty);
                if (location != null)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        location,
                        propertyName,
                        value.ToString(),
                        string.Join(", ", types));

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static object? GetPropertyValue(INamedTypeSymbol type, string propertyName)
    {
        // This is simplified - in a real implementation, you'd need to analyze
        // the constructor or property getter to determine the actual value
        // For now, we'll return null which means we can't determine the value

        // You could enhance this by:
        // 1. Looking at the constructor parameters
        // 2. Analyzing property initializers
        // 3. Checking for constant values

        return null;
    }

    private static Location? GetPropertyLocation(IPropertySymbol property)
    {
        return property.Locations.FirstOrDefault();
    }

    private static bool IsDerivedFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().Any(a =>
            string.Equals(a.AttributeClass?.Name, attributeName, StringComparison.Ordinal) ||
            string.Equals(a.AttributeClass?.Name, attributeName + nameof(Attribute), StringComparison.Ordinal));
    }

    private static AttributeData? GetAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().FirstOrDefault(a =>
            string.Equals(a.AttributeClass?.Name, attributeName, StringComparison.Ordinal) ||
            string.Equals(a.AttributeClass?.Name, attributeName + nameof(Attribute), StringComparison.Ordinal));
    }
}
