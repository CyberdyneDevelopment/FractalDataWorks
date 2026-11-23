using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Zero or one repeat (<c>?</c>).</summary>
[TypeOption(typeof(RegexQuantifiers), "Optional", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OptionalRegexQuantifier : RegexQuantifierBase
{
    /// <summary>Initializes a new instance of the <see cref="OptionalRegexQuantifier"/> class.</summary>
    public OptionalRegexQuantifier()
        : base("Optional", '?')
    {
    }

    /// <inheritdoc />
    public override (int Min, int Max, int Consumed) Read(string pattern, int index)
        => (0, 1, 1);
}
