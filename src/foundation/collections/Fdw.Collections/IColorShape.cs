namespace Fdw.Collections;

/// <summary>
/// A type option that states the colour a console draws it in (FDW-2142): one of neutral, info, success, warning or error.
/// </summary>
/// <remarks>
/// The fact is data: it is read from the option, not written into the console, and the type-collection value listing gives it as
/// <c>color</c> so a screen can draw an option it has never heard of. A colour a console does not know is drawn without tint.
/// </remarks>
public interface IColorShape
{
    /// <summary>Gets the colour name: neutral, info, success, warning or error.</summary>
    string Color { get; }
}
