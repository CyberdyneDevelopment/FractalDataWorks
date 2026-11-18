using System;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Options;

/// <summary>
/// The last name of a rule's path (FDW-6340): a kind of selection such as In or Between, with the method that follows it (To). A kind
/// states how many values it takes, so a caller never guesses from a name: a kind that takes one value is a list of one.
/// </summary>
public interface IRuleKind : IRuleLayer
{
    /// <summary>Gets the method the path ends in, after the period.</summary>
    string Method { get; }

    /// <summary>Gets the fewest values the kind takes.</summary>
    int MinValues { get; }

    /// <summary>Gets the most values the kind takes; <see langword="null"/> for a list of any length.</summary>
    int? MaxValues { get; }
}

/// <summary>
/// A kind whose method returns <typeparamref name="TOut"/> (FDW-6340). The layer before the kind fixes that type, which a caller
/// learns only when the walk reaches it.
/// </summary>
/// <typeparam name="TOut">What the method gives back: the target with the selection applied.</typeparam>
public interface IRuleKind<TOut> : IRuleKind
{
    /// <summary>Applies the kind to <paramref name="target"/> for <paramref name="field"/> with one value.</summary>
    /// <typeparam name="T">The type of the value, read from the column's data type by the caller.</typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="target">What the selection is applied to.</param>
    /// <param name="field">The column the rule is about.</param>
    /// <param name="value">The value.</param>
    /// <returns>The target with the selection applied, or the failure.</returns>
    IGenericResult<TOut> To<T>(ILogger logger, TOut target, string field, T value)
        where T : IEquatable<T>, IComparable<T>;

    /// <summary>Applies the kind to <paramref name="target"/> for <paramref name="field"/> with a list of values.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="target">What the selection is applied to.</param>
    /// <param name="field">The column the rule is about.</param>
    /// <param name="values">The values, in order.</param>
    /// <returns>The target with the selection applied, or the failure.</returns>
    IGenericResult<TOut> To<T>(ILogger logger, TOut target, string field, System.Collections.Generic.IReadOnlyList<T> values)
        where T : IEquatable<T>, IComparable<T>;

    /// <summary>Applies the kind to <paramref name="target"/> for <paramref name="field"/> with a low and a high value.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="logger">The logger.</param>
    /// <param name="target">What the selection is applied to.</param>
    /// <param name="field">The column the rule is about.</param>
    /// <param name="low">The first value.</param>
    /// <param name="high">The second value.</param>
    /// <returns>The target with the selection applied, or the failure.</returns>
    IGenericResult<TOut> To<T>(ILogger logger, TOut target, string field, T low, T high)
        where T : IEquatable<T>, IComparable<T>;
}
