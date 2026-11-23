using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>The kinds of value a record dictionary holds, each reading itself for the typed getters of the reader.</summary>
/// <remarks>The Convertible kind holds every value the others do not, so every value has a kind.</remarks>
[TypeCollection(typeof(ReaderValueKindBase), typeof(IReaderValueKind), typeof(ReaderValueKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class ReaderValueKinds : TypeCollectionBase<ReaderValueKindBase, IReaderValueKind>
{
    /// <summary>Finds the kind of a value.</summary>
    /// <param name="value">The value; a database null is <see cref="DBNull"/>.</param>
    /// <returns>The kind.</returns>
    /// <exception cref="InvalidOperationException">No kind holds the value, which the Convertible kind makes impossible.</exception>
    public static IReaderValueKind OfValue(object value)
    {
        foreach (var kind in All())
        {
            if (kind.Holds(value))
            {
                return kind;
            }
        }

        throw new InvalidOperationException($"A value of type '{value.GetType().FullName}' has no reader value kind.");
    }
}
