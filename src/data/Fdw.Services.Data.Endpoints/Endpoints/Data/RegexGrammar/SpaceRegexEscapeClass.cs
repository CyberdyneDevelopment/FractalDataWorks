using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A space (<c>\s</c>).</summary>
[TypeOption(typeof(RegexEscapeClasses), "Space", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SpaceRegexEscapeClass : RegexEscapeClassBase
{
    /// <summary>Initializes a new instance of the <see cref="SpaceRegexEscapeClass"/> class.</summary>
    public SpaceRegexEscapeClass()
        : base("Space", 's')
    {
    }

    /// <inheritdoc />
    public override char Pick(Random random) =>
        ' ';
}
