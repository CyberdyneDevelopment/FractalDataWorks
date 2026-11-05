using System;

namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>The definition of a code in force for an organization or a tenant, and where it comes from (FDW-3398).</summary>
/// <typeparam name="T">The kind of definition.</typeparam>
public sealed class EffectiveDefinition<T>
    where T : IScopedDefinition
{
    /// <summary>Initializes a new instance of the <see cref="EffectiveDefinition{T}"/> class.</summary>
    /// <param name="definition">The definition in force.</param>
    /// <param name="setAt">The level it is held at, one of <see cref="DefinitionLevels"/>.</param>
    /// <param name="lockedAt">The level that locked the code against the levels below, or null when none did.</param>
    public EffectiveDefinition(T definition, string setAt, string? lockedAt)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        Definition = definition;
        SetAt = setAt ?? throw new ArgumentNullException(nameof(setAt));
        LockedAt = lockedAt;
    }

    /// <summary>Gets the definition in force.</summary>
    public T Definition { get; }

    /// <summary>Gets the level the definition in force is held at.</summary>
    public string SetAt { get; }

    /// <summary>Gets the level that locked the code against the levels below it, or null when none did.</summary>
    public string? LockedAt { get; }
}
