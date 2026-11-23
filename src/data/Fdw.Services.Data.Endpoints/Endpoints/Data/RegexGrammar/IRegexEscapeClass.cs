using System;
using Fdw.Collections;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A backslash escape that stands for a class of characters (<c>\d</c>, <c>\w</c>, <c>\s</c>).</summary>
public interface IRegexEscapeClass : ITypeOption<int, RegexEscapeClassBase>
{
    /// <summary>Gets the character after the backslash that names this class.</summary>
    char Symbol { get; }

    /// <summary>Picks one character of the class.</summary>
    /// <param name="random">The generator.</param>
    /// <returns>A character the class matches.</returns>
    char Pick(Random random);
}
