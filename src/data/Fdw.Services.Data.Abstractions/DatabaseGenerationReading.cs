using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What a configuration gateway answers when asked which generation of its database it reaches: the generation, or
/// that its connection cannot tell.
/// </summary>
/// <remarks>
/// Two states and no empty text standing for the second: a connection of a kind that cannot tell (a file system, an
/// HTTP service) is not a failure and not a generation, and a caller that mistook one for the other would either
/// report a fault that is not there or compare a generation that does not exist.
/// </remarks>
public sealed class DatabaseGenerationReading
{
    private DatabaseGenerationReading(bool tells, string generation)
    {
        Tells = tells;
        Generation = generation;
    }

    /// <summary>Gets the reading of a connection that cannot tell which generation of its database it reaches.</summary>
    public static DatabaseGenerationReading CannotTell { get; } = new(false, string.Empty);

    /// <summary>Gets a value indicating whether the connection told a generation.</summary>
    public bool Tells { get; }

    /// <summary>Gets the generation text, or an empty text when <see cref="Tells"/> is false.</summary>
    public string Generation { get; }

    /// <summary>Makes the reading of a connection that told <paramref name="generation"/>.</summary>
    /// <param name="generation">The generation text the connection reported; it is not empty.</param>
    /// <returns>The reading.</returns>
    public static DatabaseGenerationReading Of(string generation)
        => string.IsNullOrWhiteSpace(generation)
            ? throw new ArgumentException("A generation that is told is not empty.", nameof(generation))
            : new DatabaseGenerationReading(true, generation);
}
