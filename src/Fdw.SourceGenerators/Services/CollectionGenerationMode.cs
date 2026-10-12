using System;

namespace Fdw.SourceGenerators.Services;

/// <summary>
/// Defines the different generation modes for collection classes.
/// Each mode determines the architectural pattern and instantiation strategy for the generated collection classes.
/// Generalized from enum-specific to work with any collection type.
/// </summary>
/// <remarks>
/// Pragma suppression required: source generators cannot use TypeCollections (bootstrapping problem).
/// </remarks>
#pragma warning disable FDW017
public enum CollectionGenerationMode
{
    /// <summary>
    /// Generates a static collection class with all static methods and fields.
    /// This is the most efficient pattern for read-only collections.
    /// All instances are pre-created and cached as static readonly fields.
    /// </summary>
    StaticCollection,

    /// <summary>
    /// Generates an instance-based collection using the singleton pattern.
    /// The collection class has a single static instance accessed via a property or method.
    /// Instances are created once and reused through the singleton instance.
    /// Suitable for scenarios requiring instance methods while maintaining single instantiation.
    /// </summary>
    InstanceCollection,

    /// <summary>
    /// Generates a factory-based collection that creates new instances on demand.
    /// Each call to collection methods returns newly created instances.
    /// This pattern is useful when instances need to be mutable or when
    /// different instances are required for different contexts.
    /// </summary>
    FactoryCollection,

    /// <summary>
    /// Generates a collection designed for dependency injection scenarios.
    /// The collection class implements appropriate interfaces and can be registered
    /// with DI containers. Supports both singleton and transient lifetimes
    /// depending on registration configuration.
    /// </summary>
    ServiceCollection
}
#pragma warning restore FDW017