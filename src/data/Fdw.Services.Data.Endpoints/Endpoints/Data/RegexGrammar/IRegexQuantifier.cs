using Fdw.Collections;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A quantifier that follows a regular expression atom (<c>*</c>, <c>+</c>, <c>?</c>, <c>{n}</c>, <c>{n,m}</c>).</summary>
public interface IRegexQuantifier : ITypeOption<int, RegexQuantifierBase>
{
    /// <summary>Gets the character that opens this quantifier.</summary>
    char Symbol { get; }

    /// <summary>Reads the quantifier that opens at <paramref name="index"/>.</summary>
    /// <param name="pattern">The whole pattern.</param>
    /// <param name="index">The position of the opening character.</param>
    /// <returns>The fewest and most repeats, and how many characters of the pattern the quantifier took; none taken when the opening character starts no quantifier.</returns>
    (int Min, int Max, int Consumed) Read(string pattern, int index);
}
