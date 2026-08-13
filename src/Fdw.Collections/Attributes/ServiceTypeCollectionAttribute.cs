using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a class as an immutable ServiceTypeCollection using FrozenDictionary.
/// Use for static, compile-time known service types with maximum performance.
/// </summary>
/// <remarks>
/// Generated code provides:
/// - FrozenDictionary-based lookups (O(1), immutable)
/// - Static property accessors for each ServiceTypeOption
/// - ById(), ByName(), All() methods
/// - Custom lookup methods from [TypeLookup] attributes
/// - Empty sentinel value
/// - Register(IServiceCollection, ILoggerFactory?) invoker over the swappable RegistrationMethod field
/// - Child collection support via ParentCollection parameter
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ServiceTypeCollectionAttribute : Attribute
{
    /// <summary>
    /// Defines an immutable ServiceTypeCollection.
    /// </summary>
    /// <param name="baseType">The abstract base type that all options inherit from. Supports unbound generics.</param>
    /// <param name="interfaceType">The interface that all options implement (used for return types). Supports unbound generics.</param>
    /// <param name="collectionType">The collection class itself (for self-reference).</param>
    /// <param name="parentCollection">Optional parent collection this collection belongs to. If null, this is a top-level collection.</param>
    /// <param name="name">Optional name for this collection when used as a child. Required if parentCollection is specified.</param>
    public ServiceTypeCollectionAttribute(
        Type baseType,
        Type interfaceType,
        Type collectionType,
        Type? parentCollection = null,
        string? name = null)
    {
        BaseType = baseType;
        InterfaceType = interfaceType;
        CollectionType = collectionType;
        ParentCollection = parentCollection;
        Name = name;
    }

    /// <summary>
    /// The abstract base type that all ServiceTypeOptions must inherit from.
    /// Supports unbound generics: typeof(ConnectionTypeBase&lt;,,,&gt;)
    /// </summary>
    public Type BaseType { get; }

    /// <summary>
    /// The interface type used for return values. All ServiceTypeOptions must implement this.
    /// Supports unbound generics: typeof(IConnectionType&lt;,,&gt;)
    /// </summary>
    public Type InterfaceType { get; }

    /// <summary>
    /// The collection class itself (typically typeof(ThisClass)).
    /// </summary>
    public Type CollectionType { get; }

    /// <summary>
    /// Optional parent collection this collection belongs to.
    /// When specified, this collection becomes a child of the parent and is accessible
    /// via a static property on the parent collection.
    /// </summary>
    public Type? ParentCollection { get; }

    /// <summary>
    /// The name used for the static property accessor on the parent collection.
    /// Required when ParentCollection is specified.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// If true, only discovers ServiceTypeOptions in the current compilation (not referenced assemblies).
    /// Default is false (discovers across all referenced assemblies).
    /// </summary>
    public bool RestrictToCurrentCompilation { get; set; }

    /// <summary>
    /// If true, generates an IXxxProvider interface and implementation for resolving
    /// service factories by configuration name at runtime.
    /// </summary>
    public bool GenerateProvider { get; set; }

    /// <summary>
    /// The service interface type that factories in this collection create.
    /// Required when GenerateProvider is true.
    /// </summary>
    public Type? ServiceInterface { get; set; }

    /// <summary>
    /// The configuration interface type used by services in this collection.
    /// Required when GenerateProvider is true.
    /// </summary>
    public Type? ConfigurationInterface { get; set; }

    /// <summary>
    /// The concrete configuration type used for IOptionsSnapshot binding.
    /// When specified, the generated provider constructor will use IOptionsSnapshot&lt;List&lt;ConfigurationType&gt;&gt;.
    /// If not specified, the generator will attempt to derive it from ConfigurationInterface by removing the "I" prefix.
    /// </summary>
    public Type? ConfigurationType { get; set; }

    /// <summary>
    /// The concrete provider type to use in Register (e.g., typeof(DefaultConnectionProvider)).
    /// When specified, the generator will create this provider and register factories with it.
    /// </summary>
    public Type? ProviderType { get; set; }

    /// <summary>
    /// The interface type to register the provider as (e.g., typeof(IConnectionProvider)).
    /// Required when ProviderType is specified.
    /// </summary>
    public Type? ProviderInterface { get; set; }

    /// <summary>
    /// The service category for configuration loading (e.g., "Connection", "SecretManager").
    /// When specified with a database connection, Configure() will automatically add
    /// an MsSqlConfigurationSource to load configurations from the database.
    /// </summary>
    public string? ServiceCategory { get; set; }

    /// <summary>
    /// Declares this a "declared choice" domain: multiple options register the SAME service
    /// interfaces, so exactly one is active per host, selected by configured
    /// <c>ServiceOptionType</c> — never every option at once. When true, the
    /// <c>Fdw.Services.Registration.SourceGenerators</c> module initializer registers this
    /// collection into <c>PlatformServices</c> as manual: the <c>Configure</c>/<c>Register</c>/
    /// <c>Initialize</c> phases skip it, and a host resolves the one configured option
    /// explicitly instead. Default is false (the collection participates in the collects).
    /// </summary>
    public bool Manual { get; set; }

    /// <summary>
    /// Dependency-depth layer for <c>PlatformServices</c> ordering — the domain declares its OWN layer
    /// on itself; there is no spine-side override table. Canonical layers: SecretManagers=0,
    /// Connections=1, DataGateway/DataStores=2, DataSets=3, DataVault=4, Credentials=5, Users=6,
    /// Authentication/Authorization=7. Everything else defaults to 10 (no declared dependency on the
    /// core chain). Domains sharing a value have no dependency relationship between them, so relative
    /// order within a value is provably safe regardless of arrangement.
    /// </summary>
    public int Group { get; set; } = 10;

}
