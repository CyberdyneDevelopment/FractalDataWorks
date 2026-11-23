using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One or more repeats (<c>+</c>).</summary>
[TypeOption(typeof(RegexQuantifiers), "Plus", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PlusRegexQuantifier : RegexQuantifierBase
{
    /// <summary>Initializes a new instance of the <see cref="PlusRegexQuantifier"/> class.</summary>
    public PlusRegexQuantifier()
        : base("Plus", '+')
    {
    }

    /// <inheritdoc />
    public override (int Min, int Max, int Consumed) Read(string pattern, int index)
        => (1, MaxUnboundedRepeat, 1);
}
