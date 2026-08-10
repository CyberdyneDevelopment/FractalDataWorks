using Fdw.Collections;

namespace Fdw.UI.Navigation;

/// <summary>
/// Base class for sidebar sections.
/// </summary>
/// <remarks>
/// Values are set in the constructor so the TypeCollection source generator can read them without
/// instantiation, matching every other TypeOption in the framework.
/// </remarks>
public abstract class NavSectionBase : TypeOptionBase<int, NavSectionBase>, INavSection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavSectionBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this section.</param>
    /// <param name="name">Name of the section (must match the TypeOption attribute).</param>
    /// <param name="title">The display title rendered as the section heading.</param>
    /// <param name="order">Sort order relative to other sections. Lower values appear first.</param>
    protected NavSectionBase(int id, string name, string title, int order)
        : base(id, name)
    {
        Title = title;
        Order = order;
    }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public int Order { get; }
}
