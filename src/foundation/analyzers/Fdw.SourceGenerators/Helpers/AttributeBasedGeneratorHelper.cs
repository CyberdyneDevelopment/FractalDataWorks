using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.SourceGenerators.Helpers;

/// <summary>
/// Helper class for creating ForAttributeWithMetadataName-based incremental generators.
/// Provides common patterns for attribute-driven source generation with 99x performance improvement.
/// </summary>
public static class AttributeBasedGeneratorHelper
{
    /// <summary>
    /// Creates an optimized provider for discovering types with a specific attribute.
    /// Uses ForAttributeWithMetadataName for 99x faster discovery than manual scanning.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    /// <param name="attributeFullName">The fully qualified name of the attribute (use typeof(Attr).FullName!).</param>
    /// <param name="predicate">Optional predicate to filter syntax nodes (default: ClassDeclarationSyntax or RecordDeclarationSyntax).</param>
    /// <returns>A provider that yields (TypeSymbol, Attribute) tuples.</returns>
    public static IncrementalValuesProvider<(INamedTypeSymbol? TypeSymbol, AttributeData? Attribute)> CreateAttributeProvider(
        IncrementalGeneratorInitializationContext context,
        string attributeFullName,
        Func<SyntaxNode, CancellationToken, bool>? predicate = null)
    {
        predicate ??= static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax;

        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: attributeFullName,
                predicate: predicate,
                transform: static (context, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var typeSymbol = context.TargetSymbol as INamedTypeSymbol;
                    var attribute = context.Attributes.FirstOrDefault();
                    return (TypeSymbol: typeSymbol, Attribute: attribute);
                })
            .Where(static x => x.TypeSymbol != null && x.Attribute != null);
    }

    /// <summary>
    /// Filters option types that belong to a specific collection based on a collection type extractor function.
    /// </summary>
    /// <param name="collectionClass">The collection class to match against.</param>
    /// <param name="options">All discovered option types with their attributes.</param>
    /// <param name="extractCollectionType">Function to extract the collection type from an option's attribute.</param>
    /// <param name="compilation">The compilation context.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>List of option types that belong to the specified collection.</returns>
    public static IReadOnlyList<INamedTypeSymbol> FilterRelevantOptions(
        INamedTypeSymbol collectionClass,
        ImmutableArray<(INamedTypeSymbol? TypeSymbol, AttributeData? Attribute)> options,
        Func<AttributeData, Compilation, INamedTypeSymbol?> extractCollectionType,
        Compilation compilation,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var relevantOptions = new List<INamedTypeSymbol>();

        foreach (var (typeSymbol, optionAttr) in options)
        {
            if (typeSymbol == null || optionAttr == null) continue;

            var targetCollection = extractCollectionType(optionAttr, compilation);
            if (targetCollection != null && SymbolEqualityComparer.Default.Equals(targetCollection, collectionClass))
            {
                relevantOptions.Add(typeSymbol);
            }
        }

        return relevantOptions;
    }

    /// <summary>
    /// Checks if a collection should generate code.
    /// Collections are ONLY generated in their origin assembly where they are defined.
    /// NuGet package extensibility works through:
    ///   - Origin assembly generates collection with ALL discovered options (local + referenced)
    ///   - Consumer projects get the pre-generated code from the origin package
    /// </summary>
    /// <param name="collectionClass">The collection class being evaluated.</param>
    /// <param name="relevantOptions">The options that belong to this collection.</param>
    /// <param name="compilation">Current compilation.</param>
    /// <returns>True if code should be generated for this collection.</returns>
    public static bool ShouldGenerateForCollection(
        INamedTypeSymbol collectionClass,
        IEnumerable<INamedTypeSymbol> relevantOptions,
        Compilation compilation)
    {
        // ONLY generate for collections defined in the current assembly
        // Collections from referenced assemblies are already generated in their origin assembly
        bool isCollectionInCurrentAssembly = SymbolEqualityComparer.Default.Equals(
            collectionClass.ContainingAssembly,
            compilation.Assembly);

        return isCollectionInCurrentAssembly;
    }

    /// <summary>
    /// Extracts the base type from an attribute's constructor arguments.
    /// Assumes the first constructor argument is the base type.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The base type symbol, or null if not found.</returns>
    public static INamedTypeSymbol? ExtractBaseType(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is INamedTypeSymbol baseType)
        {
            return baseType;
        }

        return null;
    }

    /// <summary>
    /// Extracts a Type argument from an attribute's constructor at a specific index.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="index">The constructor argument index.</param>
    /// <returns>The type symbol, or null if not found.</returns>
    public static INamedTypeSymbol? ExtractTypeArgument(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length > index &&
            attribute.ConstructorArguments[index].Value is INamedTypeSymbol typeSymbol)
        {
            return typeSymbol;
        }

        return null;
    }
}
