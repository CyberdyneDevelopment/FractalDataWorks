using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every regex escape class.</summary>
[ExcludeFromCodeCoverage]
public abstract class RegexEscapeClassBase : TypeOptionBase<RegexEscapeClassBase>, IRegexEscapeClass
{
    /// <summary>Initializes a new instance of the <see cref="RegexEscapeClassBase"/> class.</summary>
    /// <param name="name">The class's name.</param>
    /// <param name="symbol">The character after the backslash that names the class.</param>
    protected RegexEscapeClassBase(string name, char symbol)
        : base(name)
    {
        Symbol = symbol;
    }

    /// <inheritdoc />
    [TypeLookup("BySymbol")]
    public char Symbol { get; }

    /// <inheritdoc />
    public abstract char Pick(Random random);
}
