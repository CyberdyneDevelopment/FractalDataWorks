using Fdw.Collections;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Base class for development command categories.
/// </summary>
public abstract class DevelopmentCommandCategoryBase : TypeOptionBase<DevelopmentCommandCategoryBase>, IDevelopmentCommandCategory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentCommandCategoryBase"/> class.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="description">The category description.</param>
    protected DevelopmentCommandCategoryBase(string name, string description)
        : base(name, name, name, description, "DevelopmentCommandCategory")
    {
    }
}
