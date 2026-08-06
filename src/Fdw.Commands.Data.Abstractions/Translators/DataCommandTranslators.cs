using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Hybrid collection of data command translators.
/// Combines compile-time discovery (TypeCollection) with runtime registration (for connection-provided translators).
/// </summary>
/// <remarks>
/// <para>
/// This collection supports TWO registration mechanisms:
/// </para>
/// <para>
/// 1. Compile-Time Discovery:
/// Translators defined with [TypeOption] attribute are discovered by the source generator.
/// </para>
/// <para>
/// 2. Runtime Registration:
/// Connections register their translators at startup using Register() method.
/// Example: HttpConnection registers RestTranslator, GraphQLTranslator, GrpcTranslator.
/// </para>
/// <para>
/// Benefits:
/// <list type="bullet">
/// <item>Source-generated translators: Compile-time type safety</item>
/// <item>Runtime-registered translators: Connections bring their own translators</item>
/// <item>Unified API: GetTranslator() checks both sources</item>
/// </list>
/// </para>
/// </remarks>
[TypeCollection(typeof(DataCommandTranslatorBase<>), typeof(IDataCommandTranslator<>), typeof(DataCommandTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class DataCommandTranslators : TypeCollectionBase<DataCommandTranslatorBase<object>, IDataCommandTranslator<object>>
{
    // Runtime-registered translators (connections register these at startup)
    private static readonly ConcurrentDictionary<string, Type> _runtimeTranslators = new(System.StringComparer.OrdinalIgnoreCase);

    private static ILogger _logger = NullLogger.Instance;

    /// <summary>
    /// Sets the logger for translator operations.
    /// Call this during application startup.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public static void SetLogger(ILogger logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Registers a translator at runtime (called by connection types during registration).
    /// </summary>
    /// <param name="name">The translator name (e.g., "Rest", "GraphQL", "TSql").</param>
    /// <param name="translatorType">The translator type.</param>
    /// <remarks>
    /// This method is typically called by connection types during their registration.
    /// Example: HttpConnection registers RestTranslator, GraphQLTranslator, GrpcTranslator.
    /// </remarks>
    public static void Register(string name, Type translatorType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translator name cannot be null or whitespace", nameof(name));

        if (translatorType == null)
            throw new ArgumentNullException(nameof(translatorType));

        // Check if the type implements IDataCommandTranslator<T> for some T
        var implementsTranslator = translatorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataCommandTranslator<>));

        if (!implementsTranslator)
            throw new ArgumentException($"Type {translatorType.Name} must implement IDataCommandTranslator<TCommand>", nameof(translatorType));

        _runtimeTranslators[name] = translatorType;

        DataCommandLog.TranslatorRegistered(_logger, name, translatorType.Name);
    }

    /// <summary>
    /// Gets a translator type by domain name.
    /// Checks runtime-registered translators only (compile-time translators will be added by source generator).
    /// </summary>
    /// <param name="domainName">The domain name (e.g., "Sql", "Rest", "GraphQL").</param>
    /// <returns>The translator type, or null if not found.</returns>
    public static Type? GetTranslatorType(string domainName)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            return null;

        // Check runtime-registered translators
        if (_runtimeTranslators.TryGetValue(domainName, out var runtimeType))
            return runtimeType;

        // Future: Check compile-time translators when source generator adds ByName method
        // var translator = ByName(domainName);
        // if (translator != null)
        //     return translator.GetType();

        return null;
    }

    /// <summary>
    /// Gets all available translator types (runtime-registered only for now).
    /// Source generator will add compile-time translators when TypeOptions are discovered.
    /// </summary>
    /// <returns>An enumerable of all translator types.</returns>
    public static IEnumerable<Type> GetAllTranslatorTypes()
    {
        // Future: Add compile-time translators when source generator creates All() method
        // foreach (var translator in All())
        // {
        //     yield return translator.GetType();
        // }

        // Runtime-registered translators
        foreach (var runtimeType in _runtimeTranslators.Values)
        {
            yield return runtimeType;
        }
    }

    /// <summary>
    /// Gets all translator domain names (runtime-registered only for now).
    /// Source generator will add compile-time translators when TypeOptions are discovered.
    /// </summary>
    /// <returns>An enumerable of all translator domain names.</returns>
    public static IEnumerable<string> GetTranslatorNames()
    {
        // Future: Add compile-time translators when source generator creates All() method
        // var compileTimeNames = All().Select(t => t.DomainName);
        // return compileTimeNames.Concat(_runtimeTranslators.Keys).Distinct();

        // Runtime-registered translators only for now
        return _runtimeTranslators.Keys;
    }

    /// <summary>
    /// Checks if a translator with the given name exists.
    /// </summary>
    /// <param name="name">The translator name to check.</param>
    /// <returns>True if the translator exists; otherwise, false.</returns>
    public static bool Exists(string name)
    {
        return GetTranslatorType(name) != null;
    }
}
