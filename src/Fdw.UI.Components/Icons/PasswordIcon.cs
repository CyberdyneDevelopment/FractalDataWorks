using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Credentials and password reset.</summary>
[TypeOption(typeof(IconGlyphs), "Password")]
[ExcludeFromCodeCoverage]
public sealed class PasswordIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="PasswordIcon"/>.</summary>
    public PasswordIcon()
        : base(
            31,
            "Password",
            "0 0 24 24",
            "none",
            "currentColor",
            "1.8",
            true,
            [
                "M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
            ])
    { }
}
