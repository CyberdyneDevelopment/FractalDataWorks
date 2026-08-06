using Fdw.Collections;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Abstract base class for border styles.
/// Inherit from this class and apply [TypeOption] attribute to create custom border styles.
/// </summary>
public abstract class BorderStyleBase : TypeOptionBase<int, BorderStyleBase>, IBorderStyle
{
    /// <summary>
    /// Creates a new border style.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="name">Display name.</param>
    protected BorderStyleBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract BoxBorder Panel { get; }

    /// <inheritdoc />
    public abstract BoxBorder Input { get; }

    /// <inheritdoc />
    public abstract BoxBorder Menu { get; }

    /// <inheritdoc />
    public abstract BoxBorder Dialog { get; }

    /// <inheritdoc />
    public abstract TableBorder Table { get; }

    /// <inheritdoc />
    public abstract BoxBorder Selection { get; }
}
