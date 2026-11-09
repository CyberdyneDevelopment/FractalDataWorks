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
    private ConnectionAuthentication(string? dictionaryProperty, Func<TConfiguration, IConnectionAuthenticationType>? method)
    {
        DictionaryProperty = dictionaryProperty;
        Method = method;
    }

    /// <summary>Gets the declaration of a kind whose configuration holds no authentication dictionary.</summary>
    public static ConnectionAuthentication<TConfiguration> None { get; } = new(null, null);

    /// <summary>Gets the configuration property holding the authentication dictionary; <see langword="null"/> for <see cref="None"/>.</summary>
    public string? DictionaryProperty { get; }

    /// <summary>Gets what answers the authentication method a configuration names; <see langword="null"/> for <see cref="None"/>.</summary>
    public Func<TConfiguration, IConnectionAuthenticationType>? Method { get; }

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

        return new ConnectionAuthentication<TConfiguration>(dictionaryProperty, method);
    }
}
