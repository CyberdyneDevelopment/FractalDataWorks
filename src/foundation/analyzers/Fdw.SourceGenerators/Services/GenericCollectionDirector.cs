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
    public GenericCollectionDirector(IGenericCollectionBuilder<TId> builder)
    {
        _builder = builder;
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
        var mode = CollectionGenerationModes.StaticCollection;

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
        if (string.IsNullOrEmpty(returnType))
        {
            throw new ArgumentException("Return type cannot be null or empty.", nameof(returnType));
        }

    }

    private static ICollectionGenerationMode DetermineGenerationMode(GenericTypeInfoModel<TId> definition)
    {
        // If explicitly configured for static generation
        if (definition.GenerateStaticCollection)
        {
            return CollectionGenerationModes.StaticCollection;
        }

        // The strategy states the shape of collection it generates
        return definition.CollectionStrategy.GenerationMode;
    }
}