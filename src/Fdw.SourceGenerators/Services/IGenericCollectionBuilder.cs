using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Fdw.SourceGenerators.Models;

namespace Fdw.SourceGenerators.Services;

/// <summary>
/// Defines the contract for building collection classes using the Gang of Four Builder pattern.
/// This interface provides a fluent API for configuring and constructing collection classes
/// from type definitions and values during source generation.
/// Generalized from IEnumCollectionBuilder to work with any collection type.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public interface IGenericCollectionBuilder<TId>
    where TId : struct
{
    /// <summary>
    /// Configures the generation mode for the collection.
    /// This determines the architectural pattern and instantiation strategy for the generated class.
    /// </summary>
    /// <param name="mode">The collection generation mode to use.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid generation mode is specified.</exception>
    IGenericCollectionBuilder<TId> Configure(CollectionGenerationMode mode);

    /// <summary>
    /// Sets the type definition that contains metadata about the type to be processed.
    /// This includes namespace, class name, base type information, and generation preferences.
    /// </summary>
    /// <param name="definition">The type definition containing metadata for code generation.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> is null.</exception>
    IGenericCollectionBuilder<TId> WithDefinition(GenericTypeInfoModel<TId> definition);

    /// <summary>
    /// Sets the collection of values to include in the generated collection.
    /// These values will be used to create static fields, factory methods, and lookup functionality.
    /// </summary>
    /// <param name="values">The list of values to include in the collection.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    IGenericCollectionBuilder<TId> WithValues(IList<GenericValueInfoModel<TId>> values);

    /// <summary>
    /// Sets the return type for generated collection methods and properties.
    /// This type will be used as the return type for All(), ByName(), and other collection methods.
    /// </summary>
    /// <param name="returnType">The fully qualified or simplified return type name.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="returnType"/> is null or empty.</exception>
    IGenericCollectionBuilder<TId> WithReturnType(string returnType);

    /// <summary>
    /// Sets the compilation context for type symbol resolution and semantic analysis.
    /// This is required for resolving type references, implementing interfaces, and
    /// performing compile-time validation during source generation.
    /// </summary>
    /// <param name="compilation">The compilation context from the source generator.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="compilation"/> is null.</exception>
    IGenericCollectionBuilder<TId> WithCompilation(Compilation compilation);

    /// <summary>
    /// Sets the modifiers from the user's declared partial class.
    /// The generated partial must match what the user declared.
    /// </summary>
    /// <param name="isStatic">Whether the user's class is declared as static.</param>
    /// <param name="isAbstract">Whether the user's class is declared as abstract.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    IGenericCollectionBuilder<TId> WithUserClassModifiers(bool isStatic, bool isAbstract);

    /// <summary>
    /// Builds and returns the complete source code for the collection.
    /// The returned string contains all the necessary methods, properties, and fields
    /// for the collection based on the specified configuration.
    /// </summary>
    /// <returns>The complete source code for the collection class.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing or when the builder is in an invalid state.
    /// </exception>
    string Build();
}

/// <summary>
/// Non-generic type alias for IGenericCollectionBuilder using int IDs.
/// Provides backward compatibility for Collections and Messages generators.
/// </summary>
public interface IGenericCollectionBuilder : IGenericCollectionBuilder<int>
{
}