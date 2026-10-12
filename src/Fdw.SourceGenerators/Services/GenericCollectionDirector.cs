using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Fdw.SourceGenerators.Models;

namespace Fdw.SourceGenerators.Services;

/// <summary>
/// Director class for the Gang of Four Builder pattern that orchestrates the construction
/// of collection classes. This class determines the appropriate construction strategy
/// and manages the building process for different collection scenarios.
/// Generalized from enum-specific to work with any collection type.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public sealed class GenericCollectionDirector<TId>
    where TId : struct
{
    private readonly IGenericCollectionBuilder<TId> _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericCollectionDirector{TId}"/> class.
    /// </summary>
    /// <param name="builder">The collection builder to orchestrate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public GenericCollectionDirector(IGenericCollectionBuilder<TId> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <summary>
    /// Constructs a complete collection with all features enabled.
    /// This includes static fields, lookup methods, factory methods, and full functionality.
    /// Suitable for comprehensive collections that need all available features.
    /// </summary>
    /// <param name="definition">The type definition containing metadata for code generation.</param>
    /// <param name="values">The list of values to include in the collection.</param>
    /// <param name="returnType">The return type for generated collection methods and properties.</param>
    /// <param name="compilation">The compilation context from the source generator.</param>
    /// <returns>The generated source code for the complete collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="returnType"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the builder configuration is invalid.</exception>
    public string ConstructFullCollection(
        GenericTypeInfoModel<TId> definition,
        IList<GenericValueInfoModel<TId>> values,
        string returnType,
        Compilation compilation)
    {
        ValidateParameters(definition, values, returnType, compilation);

        var mode = DetermineGenerationMode(definition);

        return _builder
            .Configure(mode)
            .WithDefinition(definition)
            .WithValues(values)
            .WithReturnType(returnType)
            .WithCompilation(compilation)
            .Build();
    }

    /// <summary>
    /// Constructs a simplified collection for types inheriting from CollectionBase.
    /// This generates only the necessary method implementations and avoids duplicating
    /// functionality already provided by the base class.
    /// </summary>
    /// <param name="definition">The type definition containing metadata for code generation.</param>
    /// <param name="values">The list of values to include in the collection.</param>
    /// <param name="returnType">The return type for generated collection methods and properties.</param>
    /// <param name="compilation">The compilation context from the source generator.</param>
    /// <returns>The generated source code for the simplified collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="returnType"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the definition does not inherit from collection base or when the builder configuration is invalid.
    /// </exception>
    public string ConstructSimplifiedCollection(
        GenericTypeInfoModel<TId> definition,
        IList<GenericValueInfoModel<TId>> values,
        string returnType,
        Compilation compilation)
    {
        ValidateParameters(definition, values, returnType, compilation);

        if (!definition.InheritsFromCollectionBase)
        {
            throw new InvalidOperationException("Simplified collection construction requires the definition to inherit from TypeCollectionBase.");
        }

        // For simplified collections, always use static pattern as base class handles instantiation
        var mode = CollectionGenerationMode.StaticCollection;

        return _builder
            .Configure(mode)
            .WithDefinition(definition)
            .WithValues(values)
            .WithReturnType(returnType)
            .WithCompilation(compilation)
            .Build();
    }

    private static void ValidateParameters(
        GenericTypeInfoModel<TId> definition,
        IList<GenericValueInfoModel<TId>> values,
        string returnType,
        Compilation compilation)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (string.IsNullOrEmpty(returnType))
        {
            throw new ArgumentException("Return type cannot be null or empty.", nameof(returnType));
        }

        if (compilation == null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }
    }

    private static CollectionGenerationMode DetermineGenerationMode(GenericTypeInfoModel<TId> definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }
        // If explicitly configured for static generation
        if (definition.GenerateStaticCollection)
        {
            return CollectionGenerationMode.StaticCollection;
        }

        // Use CollectionStrategy to determine generation mode
        // Pragma: source generators cannot use TypeCollections (bootstrapping problem)
#pragma warning disable FDW018
        switch (definition.CollectionStrategy)
        {
            case CollectionStrategy.Factory:
                return CollectionGenerationMode.FactoryCollection;
            case CollectionStrategy.Immutable:
            case CollectionStrategy.Mutable:
            default:
                return CollectionGenerationMode.InstanceCollection;
        }
#pragma warning restore FDW018
    }
}