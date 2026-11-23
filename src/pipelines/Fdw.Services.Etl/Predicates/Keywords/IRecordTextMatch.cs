using Fdw.Collections;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A way a record predicate compares a field's text with a word.</summary>
public interface IRecordTextMatch : ITypeOption<int, RecordTextMatchBase>
{
    /// <summary>Gets the word a predicate states the comparison by.</summary>
    string Word { get; }

    /// <summary>Whether the field's text meets the comparison, ignoring case.</summary>
    /// <param name="actual">The field's text.</param>
    /// <param name="text">The text it is compared with.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(string actual, string text);
}
