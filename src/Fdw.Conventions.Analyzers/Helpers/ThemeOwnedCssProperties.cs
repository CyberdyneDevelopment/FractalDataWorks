using System;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Decides which CSS properties belong to the host theme, and whether a written value leaves the host
/// able to override it.
/// </summary>
/// <remarks>
/// <para>
/// The split exists because "inline styling" is not one defect. A style attribute carries three
/// different kinds of declaration, and only one of them is a themeability problem:
/// </para>
/// <list type="bullet">
/// <item><description><b>Geometry the component owns</b> — <c>display</c>, <c>gap</c>,
/// <c>grid-template-columns</c>, <c>padding</c>, <c>position</c>. A host skin does not re-decide
/// whether a row is a flexbox; that is the component's own arrangement, and naming it in a stylesheet
/// no other markup references only adds a hop.</description></item>
/// <item><description><b>Theme properties already routed through a token</b> —
/// <c>color:var(--n-200)</c>. The custom property IS the seam: the host defines the token, so the
/// declaration is overridable exactly as a class would be.</description></item>
/// <item><description><b>Theme properties written as a literal</b> — <c>font-size:12px</c>,
/// <c>border-radius:8px</c>, <c>letter-spacing:.05em</c>. An inline declaration outranks every normal
/// author rule, so the host cannot restyle it at all. This is the one the convention is about.</description></item>
/// </list>
/// <para>
/// The property list is deliberately short. A property is theme-owned only where a host skin
/// re-deciding it is the ordinary case; <c>transition</c> and <c>animation</c> are arguably a theme's
/// business too and are left out, because reporting them would put the rule ahead of a motion
/// contract the packages do not yet have.
/// </para>
/// </remarks>
internal static class ThemeOwnedCssProperties
{
    /// <summary>
    /// Properties whose value a host theme is expected to decide.
    /// </summary>
    private static readonly string[] Properties =
    [
        "color",
        "background",
        "background-color",
        "background-image",
        "border",
        "border-top",
        "border-right",
        "border-bottom",
        "border-left",
        "border-color",
        "border-top-color",
        "border-right-color",
        "border-bottom-color",
        "border-left-color",
        "border-width",
        "border-style",
        "border-radius",
        "border-top-left-radius",
        "border-top-right-radius",
        "border-bottom-left-radius",
        "border-bottom-right-radius",
        "box-shadow",
        "text-shadow",
        "outline",
        "outline-color",
        "font",
        "font-family",
        "font-size",
        "font-weight",
        "font-style",
        "font-variant",
        "letter-spacing",
        "line-height",
        "text-transform",
        "text-decoration",
        "fill",
        "stroke",
        "opacity",
        "filter",
        "backdrop-filter",
    ];

    /// <summary>
    /// Values that hand the decision back to the cascade instead of asserting one, so no host override
    /// is being blocked.
    /// </summary>
    private static readonly string[] DeferringValues =
    [
        "inherit",
        "initial",
        "unset",
        "revert",
        "revert-layer",
        "currentcolor",
    ];

    /// <summary>
    /// Determines whether the named property is one the host theme owns.
    /// </summary>
    /// <param name="property">The declaration's property name, lower-cased.</param>
    /// <returns><see langword="true"/> when the property is theme-owned; otherwise <see langword="false"/>.</returns>
    internal static bool IsThemeOwned(string property)
    {
        foreach (var candidate in Properties)
        {
            if (string.Equals(property, candidate, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the host can still override the written value.
    /// </summary>
    /// <param name="value">The declaration's value, trimmed.</param>
    /// <returns><see langword="true"/> when the value leaves the decision to the host; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// A <c>var(--token)</c> reference names the theme's own contract, so the host decides what it
    /// resolves to. A cascade keyword defers outright. Everything else — a hex colour, a pixel size, a
    /// keyword such as <c>uppercase</c> or <c>none</c> — is this markup asserting the value, and an
    /// inline assertion is the one thing a stylesheet cannot outrank.
    /// </remarks>
    internal static bool IsHostOverridable(string value)
    {
        if (value.IndexOf("var(--", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        foreach (var keyword in DeferringValues)
        {
            if (string.Equals(value, keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
