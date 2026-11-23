using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A letter, digit or underscore (<c>\w</c>).</summary>
[TypeOption(typeof(RegexEscapeClasses), "Word", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WordRegexEscapeClass : RegexEscapeClassBase
{
    private const string Pool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

    /// <summary>Initializes a new instance of the <see cref="WordRegexEscapeClass"/> class.</summary>
    public WordRegexEscapeClass()
        : base("Word", 'w')
    {
    }

    /// <inheritdoc />
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public override char Pick(Random random) =>
        Pool[random.Next(Pool.Length)];
}
