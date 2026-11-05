using System;
using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// A threshold of one type (FDW-3936): an ordered list of rules, each a condition on the values the type reports and the consequence that fires when it holds. For example
/// "consecutive failures at least 3: Alert; at least 5: DisablePipeline". Every rule that matches fires, in the order they are stated.
/// </summary>
/// <typeparam name="T">The threshold type, which names the subject and the fields the rules may name.</typeparam>
public sealed class Threshold<T> : IThresholdDefinition
    where T : IThreshold
{
    /// <summary>Initializes a new instance of the <see cref="Threshold{T}"/> class.</summary>
    /// <param name="type">The threshold type.</param>
    /// <param name="name">The threshold name.</param>
    /// <param name="isEnabled">Whether it is judged at all.</param>
    /// <param name="firesOnEntry">Whether a rule fires only when its criteria start to hold.</param>
    /// <param name="parameters">The parameters it states, by name.</param>
    /// <param name="rules">The rules, in the order they are judged.</param>
    public Threshold(T type, string name, bool isEnabled, bool firesOnEntry, IReadOnlyDictionary<string, object?> parameters, IReadOnlyList<ThresholdRule> rules)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The value is required.", nameof(name));
        }
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name;
        IsEnabled = isEnabled;
        FiresOnEntry = firesOnEntry;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>Gets the threshold type.</summary>
    public T Type { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; }

    /// <inheritdoc/>
    public bool FiresOnEntry { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ThresholdRule> Rules { get; }

    IThreshold IThresholdDefinition.Type => Type;
}
