using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;

namespace Fdw.ServiceTypes;

/// <summary>
/// Resolves a service type's tuning default (FDW-3823), in this order: the host's override for the type, the host's server-wide override
/// for its category, then the value the option declares (<see cref="IDeclaresDefaults"/>, its own over its domain's shared values). A value
/// the configuration states is used before all of them, by the caller.
/// </summary>
/// <remarks>
/// Why the override is held here and not on the option: an option is one registered singleton built in a module initializer, shared by every
/// reader; changing its state would make the lookup's answer depend on call order. A host records an override from a Configure body
/// (<c>option.AppendConfiguration(...)</c> or the category's Configure), which runs before Register and before any create reads it; a second
/// run records the same value, so it is idempotent.
/// </remarks>
public static class ServiceTypeDefaults
{
    private static readonly ConcurrentDictionary<string, object> Overrides = new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, object> CategoryOverrides = new(StringComparer.Ordinal);

    /// <summary>Records a host's value for a type's tuning default, read before the value the type declares.</summary>
    /// <param name="option">The type option: a service type, or any other option that declares defaults (FDW-3956).</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="value">The host's value.</param>
    public static void Override(ITypeOption option, string field, object value)
    {
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("A default names the configuration property it is for.", nameof(field));
        Overrides[Key(option, field)] = value;
    }

    /// <summary>
    /// Records a host's server-wide value for a tuning default of every type in a category (for example which named resilience policy a
    /// connection uses), read after a type's own host override and before the value a type declares.
    /// </summary>
    /// <param name="category">The service category, as the options name it (Connection, SecretManager...).</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="value">The host's value.</param>
    public static void OverrideForCategory(string category, string field, object value)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("A category override names its category.", nameof(category));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("A default names the configuration property it is for.", nameof(field));
        CategoryOverrides[category + "|" + field] = value;
    }

    /// <summary>Resolves a type's tuning default for a field its configuration does not state.</summary>
    /// <param name="option">The type option: a service type, or any other option that declares defaults (FDW-3956).</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="value">The host's override, else the type's declared value; <see langword="null"/> when the type declares none.</param>
    /// <returns><see langword="true"/> when the host or the type gives a value.</returns>
    public static bool TryResolve(ITypeOption option, string field, out object? value)
    {
        if (Overrides.TryGetValue(Key(option, field), out var host))
        {
            value = host;
            return true;
        }

        // Why a blank category skips this step: the category override is keyed by the category the option states, and an option that states
        // none has no key to read; none is guessed for it (FDW-3956).
        if (!string.IsNullOrWhiteSpace(option.Category) && CategoryOverrides.TryGetValue(option.Category + "|" + field, out var server))
        {
            value = server;
            return true;
        }

        if (option is IDeclaresDefaults declares && declares.DeclaredDefaults.TryGetValue(field, out var declared))
        {
            value = declared;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Builds a type's declared defaults from the ones its domain declares, with the type's own values added or replacing them.
    /// </summary>
    /// <param name="inherited">The domain base's declared defaults.</param>
    /// <param name="own">The type's own values, by configuration property name.</param>
    /// <returns>The merged defaults.</returns>
    public static IReadOnlyDictionary<string, object> With(IReadOnlyDictionary<string, object> inherited, params (string Field, object Value)[] own)
    {
        var merged = inherited.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (var (field, value) in own)
        {
            merged[field] = value ?? throw new ArgumentException($"The default for {field} has no value.", nameof(own));
        }

        return merged;
    }

    private static string Key(ITypeOption option, string field)
        => (option.GetType().FullName ?? throw new InvalidOperationException("A service type option has no full name; only an open generic parameter lacks one."))
           + "|" + field;
}
