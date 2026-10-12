using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Fdw.CodeBuilder.CSharp.Builders;
using Fdw.SourceGenerators.Configuration;
using Fdw.SourceGenerators.Models;

namespace Fdw.SourceGenerators.Generators;

/// <summary>
/// Generates field declarations for collection classes.
/// Responsible for creating _all, _empty, and lookup dictionary fields.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public sealed class FieldGenerator<TId>
    where TId : struct
{
    private readonly CollectionBuilderConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldGenerator{TId}"/> class.
    /// </summary>
    /// <param name="config">The configuration for collection generation.</param>
    public FieldGenerator(CollectionBuilderConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Generates the _all static field (FrozenDictionary, ConcurrentDictionary, or Dictionary based on strategy).
    /// </summary>
    /// <param name="keyType">The key type for the dictionary (e.g., "int", "string", "System.Guid").</param>
    /// <param name="returnType">The value type for the dictionary.</param>
    /// <param name="strategy">The collection strategy determining dictionary type.</param>
    public static IFieldBuilder GenerateAllField(string keyType, string returnType, CollectionStrategy strategy)
    {
#pragma warning disable FDW018 // Source generator internal enum — bootstrapping prevents TypeCollection usage
        string dictionaryType = strategy switch
        {
            CollectionStrategy.Immutable => $"FrozenDictionary<{keyType}, {returnType}>",
            CollectionStrategy.Mutable => $"ConcurrentDictionary<{keyType}, {returnType}>",
            CollectionStrategy.Factory => $"Dictionary<{keyType}, {returnType}>",
            _ => $"FrozenDictionary<{keyType}, {returnType}>"
        };
#pragma warning restore FDW018

        var builder = new FieldBuilder()
            .WithName("_all")
            .WithType(dictionaryType)
            .WithAccessModifier("private")
            .AsStatic();

        // Only readonly for Immutable strategy
        if (strategy == CollectionStrategy.Immutable)
        {
            builder.AsReadOnly();
        }

        builder.WithXmlDoc("Primary lookup dictionary for all collection items, keyed by ID.");

        return builder;
    }

    /// <summary>
    /// Generates the _empty static field (initialized in static constructor).
    /// </summary>
    public static IFieldBuilder GenerateEmptyField(string returnType)
    {
        return new FieldBuilder()
            .WithName("_empty")
            .WithType(returnType)
            .WithAccessModifier("private")
            .AsStatic()
            .AsReadOnly()
            .WithXmlDoc("Static empty instance with default values.");
    }

    /// <summary>
    /// Generates lookup dictionary fields for non-ID properties (e.g., _byName).
    /// Creates separate dictionary fields for each lookup property based on strategy.
    /// </summary>
    public static IList<IFieldBuilder> GenerateLookupDictionaryFields(
        GenericTypeInfoModel<TId> definition,
        string returnType)
    {
        var fields = new List<IFieldBuilder>();

        if (definition?.LookupProperties == null)
            return fields;

        // Get key type from definition (defaults to "int" if not specified)
        var keyType = definition.KeyType ?? "int";

        var nonIdLookups = definition.LookupProperties
            .Where(l => !string.Equals(l.PropertyName, "Id", StringComparison.Ordinal) ||
                       !string.Equals(l.PropertyType, keyType, StringComparison.Ordinal))
            .ToList();

        if (nonIdLookups.Count == 0)
            return fields;

        // Determine dictionary type based on strategy
        var strategy = definition.CollectionStrategy;

        // Generate lookup dictionary fields for all non-ID lookups
        foreach (var lookup in nonIdLookups)
        {
#pragma warning disable FDW018 // Source generator internal enum — bootstrapping prevents TypeCollection usage
            string dictionaryType = strategy switch
            {
                CollectionStrategy.Immutable => $"FrozenDictionary<{lookup.PropertyType}, {returnType}>",
                CollectionStrategy.Mutable => $"ConcurrentDictionary<{lookup.PropertyType}, {returnType}>",
                CollectionStrategy.Factory => $"Dictionary<{lookup.PropertyType}, {returnType}>",
                _ => $"FrozenDictionary<{lookup.PropertyType}, {returnType}>"
            };
#pragma warning restore FDW018

            var fieldBuilder = new FieldBuilder()
                .WithName($"_by{lookup.PropertyName}")
                .WithType(dictionaryType)
                .WithAccessModifier("private")
                .AsStatic();

            // Only readonly for Immutable strategy
            if (strategy == CollectionStrategy.Immutable)
            {
                fieldBuilder.AsReadOnly();
            }

            fieldBuilder.WithXmlDoc($"Lookup dictionary for {lookup.PropertyName}-based searches.");

            fields.Add(fieldBuilder);
        }

        return fields;
    }

    /// <summary>
    /// Generates static fields for enum values (field-per-value pattern).
    /// </summary>
    public static IList<IFieldBuilder> GenerateValueFields(
        IList<GenericValueInfoModel<TId>> values,
        string returnType)
    {
        var fields = new List<IFieldBuilder>();

        foreach (var value in values)
        {
            // Skip abstract types - they can't be instantiated
            if (value.IsAbstract)
                continue;

            var field = new FieldBuilder()
                .WithName(value.Name)
                .WithType(returnType)
                .WithAccessModifier("public")
                .AsStatic()
                .AsReadOnly()
                .WithXmlDoc($"Gets the {value.Name} instance.");

            fields.Add(field);
        }

        return fields;
    }
}
