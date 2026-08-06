using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;

namespace Fdw.UI.Registration;

/// <summary>
/// Base class for a group of pages contributed to <see cref="PageTypes"/> by one package.
/// </summary>
// Why: the pages arrive through the constructor, like every other FDW option's values. The previous shape
// declared an Assembly here and carried nav entries as an overridden property initializer defaulting to
// an empty list — so a group contributing no nav registered itself and then said nothing, which is how
// eight real pages ended up with no sidebar entry and nobody noticed.
public abstract class PageTypeBase : TypeOptionBase<int, PageTypeBase>, IPageType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageTypeBase"/> class.
    /// </summary>
    /// <param name="id">The option id, unique within <see cref="PageTypes"/>.</param>
    /// <param name="name">The option name.</param>
    /// <param name="pages">The pages this group contributes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pages"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pages"/> is empty, or two pages share a name.</exception>
    protected PageTypeBase(int id, string name, IReadOnlyList<IPage> pages)
        : base(id, name)
    {
        if (pages is null)
            throw new ArgumentNullException(nameof(pages));

        // Why: a group that contributes no pages is a declaration mistake, not a valid state — it
        // registers into the collection and then supplies nothing.
        if (pages.Count == 0)
            throw new ArgumentException($"Page type '{name}' declares no pages.", nameof(pages));

        var duplicate = pages.GroupBy(p => p.Name, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Page type '{name}' declares more than one page named '{duplicate.Key}'.", nameof(pages));

        Pages = pages;
        PageAssemblies = pages.Select(p => p.Component.Assembly).Distinct().ToList();
    }

    /// <summary>Initializes the collection's NotFound sentinel, which contributes no pages.</summary>
    /// <remarks>
    /// Why the empty-pages rule does not apply here: TypeCollectionGenerator builds an Empty/NotFound
    /// sentinel for every collection and prefers a protected parameterless constructor when one exists.
    /// Without this it fell through to the validating constructor with an empty list and threw
    /// "Page type '_Empty' declares no pages" from the static initializer of PageTypes — inside a module
    /// initializer, so EVERY app that registers UI page types died at startup before Main ran. The
    /// validation itself is right: a declared page group contributing nothing IS a mistake. The sentinel
    /// is not a declared group — it is the collection's "no such page type" answer, and contributing
    /// nothing is exactly what it means.
    /// </remarks>
    protected PageTypeBase()
        : base(0, "NotFound")
    {
        Pages = [];
        PageAssemblies = [];
    }

    /// <inheritdoc />
    public IReadOnlyList<IPage> Pages { get; }

    /// <inheritdoc />
    public IReadOnlyList<Assembly> PageAssemblies { get; }
}
