using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL column types a logical field type name maps to.</summary>
/// <remarks>A word no type answers to gives the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(SqlTypeWordBase), typeof(ISqlTypeWord), typeof(SqlTypeWords))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlTypeWords : TypeCollectionBase<SqlTypeWordBase, ISqlTypeWord>
{
    /// <summary>Finds the type a field type name states.</summary>
    /// <param name="word">The field type name, in any case.</param>
    /// <returns>The type, or <c>NotFound</c>.</returns>
    public static ISqlTypeWord OfWord(string? word)
    {
        foreach (var type in All())
        {
            if (type.Words.Any(known => string.Equals(known, word, StringComparison.OrdinalIgnoreCase)))
            {
                return type;
            }
        }

        return NotFound;
    }
}
