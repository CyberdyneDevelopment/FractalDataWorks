using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Fdw.CodeBuilder.Abstractions;
using Fdw.CodeBuilder.CSharp.Builders;
using Fdw.SourceGenerators.Configuration;
using Fdw.SourceGenerators.Models;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Generators;

/// <summary>
/// Generates static constructors for collection classes.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public sealed class StaticConstructorGenerator<TId>
    where TId : struct
{
    private readonly CollectionBuilderConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticConstructorGenerator{TId}"/> class.
    /// </summary>
    /// <param name="config">The configuration for collection generation.</param>
    public StaticConstructorGenerator(CollectionBuilderConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Generates the static constructor that initializes _all and _empty fields.
    /// </summary>
#pragma warning disable MA0051 // Sequential static constructor body building for TypeCollection registration
    public static IConstructorBuilder GenerateStaticConstructor(
        GenericTypeInfoModel<TId> definition,
        System.Collections.Generic.IList<GenericValueInfoModel<TId>> values,
        string returnType,
        Compilation compilation)
    {
        var constructorBody = new StringBuilder();

        var includedValues = values.Where(v => v.Include && !v.IsAbstract && !v.IsStatic && !v.IsGenericType).ToList();

        // Get key type from definition (defaults to "int" if not specified)
        var keyType = definition.KeyType ?? "int";
        var strategy = definition.CollectionStrategy;

        if (includedValues.Count > 0)
        {
            // Build dictionary of instances keyed by their Id property
            constructorBody.AppendLine("var dictionary = new System.Collections.Generic.Dictionary<" + keyType + ", " + returnType + ">();");
            constructorBody.AppendLine();

            // Create instances and add to dictionary
            foreach (var value in includedValues)
            {
                if (value.BaseConstructorId.HasValue)
                {
                    // Use literal ID from base constructor argument
                    constructorBody.AppendLine($"        dictionary.Add({value.BaseConstructorId.Value}, new {value.ShortTypeName}());");
                }
                else
                {
                    // Fallback: instantiate and read Id property
                    var varName = value.Name.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                    constructorBody.AppendLine($"        var {varName} = new {value.ShortTypeName}();");
                    constructorBody.AppendLine($"        dictionary.Add({varName}.Id, {varName});");
                }
                constructorBody.AppendLine();
            }

            // Convert to appropriate dictionary type based on strategy
#pragma warning disable FDW018
            switch (strategy)
            {
                case CollectionStrategy.Immutable:
                    constructorBody.AppendLine("_all = dictionary.ToFrozenDictionary();");
                    break;
                case CollectionStrategy.Mutable:
                    constructorBody.AppendLine("_all = new System.Collections.Concurrent.ConcurrentDictionary<" + keyType + ", " + returnType + ">(dictionary);");
                    break;
                case CollectionStrategy.Factory:
                    constructorBody.AppendLine("_all = dictionary;");
                    break;
            }
#pragma warning restore FDW018
        }
        else
        {
            // No values - create empty dictionary based on strategy
#pragma warning disable FDW018
            switch (strategy)
            {
                case CollectionStrategy.Immutable:
                    constructorBody.AppendLine("_all = System.Collections.Frozen.FrozenDictionary<" + keyType + ", " + returnType + ">.Empty;");
                    break;
                case CollectionStrategy.Mutable:
                    constructorBody.AppendLine("_all = new System.Collections.Concurrent.ConcurrentDictionary<" + keyType + ", " + returnType + ">();");
                    break;
                case CollectionStrategy.Factory:
                    constructorBody.AppendLine("_all = new System.Collections.Generic.Dictionary<" + keyType + ", " + returnType + ">();");
                    break;
            }
#pragma warning restore FDW018
        }

        constructorBody.AppendLine();

        // Initialize _empty field
        var baseTypeName = definition.ClassName;

        // Try to resolve the base type symbol using metadata format for generic types
        INamedTypeSymbol? baseTypeSymbol = null;
        if (!string.IsNullOrEmpty(definition.FullTypeName))
        {
            var fullTypeName = definition.FullTypeName;
            var genericIndex = fullTypeName.IndexOf('<');

            if (genericIndex > 0)
            {
                // Generic type - extract metadata name
                var baseName = fullTypeName.Substring(0, genericIndex);
                var typeParamSection = fullTypeName.Substring(genericIndex);
                var arity = typeParamSection.Count(c => c == ',') + 1;
                var metadataName = $"{baseName}`{arity}";
                baseTypeSymbol = compilation.GetTypeByMetadataName(metadataName);
            }
            else
            {
                // Non-generic type
                baseTypeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
            }
        }

        // Fallback to simple name resolution
        if (baseTypeSymbol == null)
        {
            var fullyQualifiedTypeName = baseTypeName.Contains(".")
                ? baseTypeName
                : $"{definition.Namespace}.{baseTypeName}";
            baseTypeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
        }

        if (baseTypeSymbol != null && baseTypeSymbol.TypeKind == TypeKind.Class && !GenericTypeHelper.IsGenericType(baseTypeSymbol))
        {
            // Non-generic base class exists - use EmptyClassName instance
            var emptyClassName = $"Empty{baseTypeName}";
            constructorBody.AppendLine($"_empty = new {emptyClassName}();");
        }
        else
        {
            // Interface only OR generic base type - use null!
            // For generic types, we cannot instantiate an open generic, so _empty is null
            constructorBody.AppendLine("_empty = null!;");
        }

        // Initialize lookup dictionaries for non-ID properties
        if (definition.LookupProperties != null && definition.LookupProperties.Length > 0)
        {
            // Get key type from definition (defaults to "int" if not specified)
            var lookupKeyType = definition.KeyType ?? "int";

            var nonIdLookups = definition.LookupProperties
                .Where(l => !string.Equals(l.PropertyName, "Id", StringComparison.Ordinal) ||
                           !string.Equals(l.PropertyType, lookupKeyType, StringComparison.Ordinal))
                .ToList();

            if (nonIdLookups.Count > 0)
            {
                constructorBody.AppendLine();

                foreach (var lookup in nonIdLookups)
                {
                    var dictionaryName = $"_by{lookup.PropertyName}";

                    // Use appropriate dictionary conversion based on strategy
#pragma warning disable FDW018
                    switch (strategy)
                    {
                        case CollectionStrategy.Immutable:
                            constructorBody.AppendLine($"{dictionaryName} = _all.Values.ToFrozenDictionary(x => x.{lookup.PropertyName});");
                            break;
                        case CollectionStrategy.Mutable:
                            constructorBody.AppendLine($"{dictionaryName} = new System.Collections.Concurrent.ConcurrentDictionary<{lookup.PropertyType}, {returnType}>(_all.Values.ToDictionary(x => x.{lookup.PropertyName}));");
                            break;
                        case CollectionStrategy.Factory:
                            constructorBody.AppendLine($"{dictionaryName} = _all.Values.ToDictionary(x => x.{lookup.PropertyName});");
                            break;
                    }
#pragma warning restore FDW018
                }
            }
        }

        var constructor = new ConstructorBuilder()
            .WithClassName(definition.CollectionName)
            .AsStatic()
            .WithBody(constructorBody.ToString());

        return constructor;
    }
#pragma warning restore MA0051
}
