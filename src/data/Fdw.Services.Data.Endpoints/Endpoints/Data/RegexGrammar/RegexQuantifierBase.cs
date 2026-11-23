using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every regex quantifier.</summary>
[ExcludeFromCodeCoverage]
public abstract class RegexQuantifierBase : TypeOptionBase<RegexQuantifierBase>, IRegexQuantifier
{
    /// <summary>How many times an unbounded quantifier repeats at most.</summary>
    protected const int MaxUnboundedRepeat = 8;

    /// <summary>Initializes a new instance of the <see cref="RegexQuantifierBase"/> class.</summary>
    /// <param name="name">The quantifier's name.</param>
    /// <param name="symbol">The character that opens the quantifier.</param>
    protected RegexQuantifierBase(string name, char symbol)
        : base(name)
    {
        Symbol = symbol;
    }

    /// <inheritdoc />
    [TypeLookup("BySymbol")]
    public char Symbol { get; }

    /// <inheritdoc />
    public abstract (int Min, int Max, int Consumed) Read(string pattern, int index);
}
