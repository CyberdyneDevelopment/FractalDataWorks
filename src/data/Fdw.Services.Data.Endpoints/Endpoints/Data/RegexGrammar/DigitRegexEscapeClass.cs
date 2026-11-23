using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A decimal digit (<c>\d</c>).</summary>
[TypeOption(typeof(RegexEscapeClasses), "Digit", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DigitRegexEscapeClass : RegexEscapeClassBase
{
    /// <summary>Initializes a new instance of the <see cref="DigitRegexEscapeClass"/> class.</summary>
    public DigitRegexEscapeClass()
        : base("Digit", 'd')
    {
    }

    /// <inheritdoc />
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public override char Pick(Random random) =>
        (char)('0' + random.Next(10));
}
