using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Base class for semantic status colors. Each color carries the dot class and the theme token that
/// draw it, so a component says which tone a value takes and never which css that tone is.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class StatusColorBase : TypeOptionBase<int, StatusColorBase>, IStatusColor
{
    /// <summary>
    /// Initializes a new instance of <see cref="StatusColorBase"/>.
    /// </summary>
    /// <param name="id">The option id.</param>
    /// <param name="name">The option name.</param>
    /// <param name="dotClass">The class that colours a status dot in this tone.</param>
    /// <param name="tokenReference">The theme custom-property reference for text, as written in a css value.</param>
    /// <param name="dotTokenReference">The theme custom-property reference for a dot, as written in a css value.</param>
    protected StatusColorBase(int id, string name, string dotClass, string tokenReference, string dotTokenReference)
        : base(id, name)
    {
        this.DotClass = dotClass;
        this.TokenReference = tokenReference;
        this.DotTokenReference = dotTokenReference;
    }

    /// <inheritdoc/>
    public string DotClass { get; }

    /// <inheritdoc/>
    public string TokenReference { get; }

    /// <inheritdoc/>
    public string DotTokenReference { get; }
}
