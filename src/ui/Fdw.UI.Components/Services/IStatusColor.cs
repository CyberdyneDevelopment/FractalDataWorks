using Fdw.Collections;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Interface for semantic status colors. Each color carries the css the console draws it with, so the
/// tone-to-css mapping exists once instead of per component.
/// </summary>
public interface IStatusColor : ITypeOption<int, StatusColorBase>
{
    /// <summary>
    /// Gets the class that colours a status dot in this tone.
    /// </summary>
    string DotClass { get; }

    /// <summary>
    /// Gets the theme custom-property reference for text in this tone, as written in a css value.
    /// </summary>
    string TokenReference { get; }

    /// <summary>
    /// Gets the theme custom-property reference for a dot drawn in this tone, as written in a css value.
    /// </summary>
    /// <remarks>
    /// It differs from <see cref="TokenReference"/> only on the neutral tones, where the console draws
    /// the dot one step dimmer than the text beside it. On a coloured tone the two are the same.
    /// </remarks>
    string DotTokenReference { get; }
}
