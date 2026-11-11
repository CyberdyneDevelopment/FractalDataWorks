using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// What a connection kind states about the authentication values its configuration holds (FDW-4607): either it holds none
/// (<see cref="None"/>), or it holds them in one string dictionary and says which authentication method that dictionary belongs to
/// (<see cref="Of"/>). Every kind states one, so a kind cannot be added without taking a position.
/// </summary>
/// <typeparam name="TConfiguration">The kind's configuration class.</typeparam>
public sealed class ConnectionAuthentication<TConfiguration>
{
    private ConnectionAuthentication(
        string? dictionaryProperty,
        Func<TConfiguration, IConnectionAuthenticationType>? method,
        Func<TConfiguration, IReadOnlyList<string>>? sharedProperties)
    {
        DictionaryProperty = dictionaryProperty;
        Method = method;
        SharedProperties = sharedProperties;
    }

    /// <summary>Gets the declaration of a kind whose configuration holds no authentication dictionary.</summary>
    public static ConnectionAuthentication<TConfiguration> None { get; } = new(null, null, null);

    /// <summary>Gets the configuration property holding the authentication dictionary; <see langword="null"/> for <see cref="None"/>.</summary>
    public string? DictionaryProperty { get; }

    /// <summary>Gets what answers the authentication method a configuration names; <see langword="null"/> for <see cref="None"/>.</summary>
    public Func<TConfiguration, IConnectionAuthenticationType>? Method { get; }

    /// <summary>
    /// Gets what answers the names of the plain (not secret) settings another declared owner keeps in the same dictionary, such as an HTTP
    /// connection's paging style (FDW-4938); <see langword="null"/> when the dictionary holds authentication values only.
    /// </summary>
    public Func<TConfiguration, IReadOnlyList<string>>? SharedProperties { get; }

    /// <summary>
    /// Gets the string dictionaries of <typeparamref name="TConfiguration"/> that this declaration does not account for: all of them for
    /// <see cref="None"/>, none for <see cref="Of"/>. A kind that declares <see cref="None"/> and has one is refused where it is used.
    /// </summary>
    public IReadOnlyList<string> UndeclaredDictionaries => DictionaryProperty is not null
        ? []
        : typeof(TConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => typeof(IDictionary<string, string?>).IsAssignableFrom(property.PropertyType))
            .Select(property => property.Name)
            .ToList();

    /// <summary>Declares the authentication dictionary of a kind and the method it belongs to.</summary>
    /// <param name="dictionaryProperty">The <typeparamref name="TConfiguration"/> property holding the authentication dictionary.</param>
    /// <param name="method">Answers the authentication method the configuration names.</param>
    public static ConnectionAuthentication<TConfiguration> Of(string dictionaryProperty, Func<TConfiguration, IConnectionAuthenticationType> method)
    {
        if (string.IsNullOrWhiteSpace(dictionaryProperty)) throw new ArgumentException("A dictionary property is required.", nameof(dictionaryProperty));
        if (method is null) throw new ArgumentNullException(nameof(method));

        return new ConnectionAuthentication<TConfiguration>(dictionaryProperty, method, null);
    }

    /// <summary>
    /// Declares that the authentication dictionary also holds the plain settings of a second owner the configuration names (FDW-4938: an
    /// HTTP connection's paging style keeps its page size and parameter names beside the authentication values). A key that owner expects is
    /// accepted when the configuration is saved and shown as it is; a key neither the method nor that owner expects is still refused and
    /// masked, and a key the method expects stays the method's to mask.
    /// </summary>
    /// <param name="sharedProperties">Answers the names the second owner a configuration names expects; empty when it names none.</param>
    /// <returns>The declaration with the second owner.</returns>
    public ConnectionAuthentication<TConfiguration> SharedWith(Func<TConfiguration, IReadOnlyList<string>> sharedProperties)
    {
        if (sharedProperties is null) throw new ArgumentNullException(nameof(sharedProperties));
        if (DictionaryProperty is null || Method is null)
            throw new InvalidOperationException("A kind that declares no authentication dictionary has no dictionary to share.");

        return new ConnectionAuthentication<TConfiguration>(DictionaryProperty, Method, sharedProperties);
    }
}
