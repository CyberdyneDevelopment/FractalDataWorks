using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Zero or more repeats (<c>*</c>).</summary>
[TypeOption(typeof(RegexQuantifiers), "Star", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StarRegexQuantifier : RegexQuantifierBase
{
    /// <summary>Initializes a new instance of the <see cref="StarRegexQuantifier"/> class.</summary>
    public StarRegexQuantifier()
        : base("Star", '*')
    {
    }

    /// <inheritdoc />
    public override (int Min, int Max, int Consumed) Read(string pattern, int index)
        => (0, MaxUnboundedRepeat, 1);
}
