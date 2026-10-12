#pragma warning disable CS1591
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Keys;

/// <summary>
/// Concrete implementation of <see cref="IKeyDefinition{TProperty}"/>.
/// </summary>
/// <typeparam name="TProperty">The property definition type.</typeparam>
[ExcludeFromCodeCoverage]
public sealed class KeyDefinition<TProperty> : IKeyDefinition<TProperty>
    where TProperty : IPropertyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyDefinition{TProperty}"/> class.
    /// </summary>
    /// <param name="members">The key members in ordinal order.</param>
    /// <param name="name">The optional constraint name.</param>
    public KeyDefinition(IReadOnlyList<KeyMember> members, string? name = null)
    {
        Members = members;
        Name = name;
    }

    /// <inheritdoc/>
    public string? Name { get; }

    /// <inheritdoc/>
    public IReadOnlyList<KeyMember> Members { get; }

    /// <inheritdoc/>
    public bool IsComposite => Members.Count > 1;
}
