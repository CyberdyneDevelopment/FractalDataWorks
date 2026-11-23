using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>An explicit repeat count (<c>{n}</c> or <c>{n,m}</c>).</summary>
[TypeOption(typeof(RegexQuantifiers), "Count", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CountRegexQuantifier : RegexQuantifierBase
{
    /// <summary>Initializes a new instance of the <see cref="CountRegexQuantifier"/> class.</summary>
    public CountRegexQuantifier()
        : base("Count", '{')
    {
    }

    /// <inheritdoc />
    public override (int Min, int Max, int Consumed) Read(string pattern, int index)
    {
        var close = pattern.IndexOf('}', index + 1);
        if (close > index)
        {
            var body = pattern[(index + 1)..close];
            var parts = body.Split(',');
            var min = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var max = parts.Length > 1 && parts[1].Length > 0
                ? int.Parse(parts[1], CultureInfo.InvariantCulture)
                : (parts.Length > 1 ? min + MaxUnboundedRepeat : min);
            return (min, max, close - index + 1);
        }

        return (1, 1, 0);
    }
}
