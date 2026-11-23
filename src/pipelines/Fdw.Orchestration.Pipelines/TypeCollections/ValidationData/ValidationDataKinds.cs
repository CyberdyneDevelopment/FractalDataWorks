using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>
/// The kinds of value the DataType validation rule checks text against: whole numbers, decimals, floating point numbers,
/// booleans, dates, identifiers and text.
/// </summary>
/// <remarks>
/// A rule names a kind by any of its words (<c>INT</c>, <c>INTEGER</c>, <c>INT32</c> name one kind), compared without regard
/// to case through <see cref="ByWord"/>; a word that names none gets no kind, and no text is valid for it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ValidationDataKindBase), typeof(IValidationDataKind), typeof(ValidationDataKinds))]
public abstract partial class ValidationDataKinds : TypeCollectionBase<ValidationDataKindBase, IValidationDataKind>
{
    /// <summary>Finds the kind a word names.</summary>
    /// <param name="word">The word, in any case.</param>
    /// <returns>The kind, or <c>null</c> when no kind has the word.</returns>
    public static IValidationDataKind? ByWord(string word)
        => All().FirstOrDefault(kind => kind.Words.Contains(word.ToUpperInvariant(), StringComparer.Ordinal));
}
