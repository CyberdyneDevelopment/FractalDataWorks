using Fdw.Commands.Development.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Base class for Roslyn command categories.
/// Extends <see cref="DevelopmentCommandCategoryBase"/> for C# specific categorization.
/// </summary>
public abstract class RoslynCommandCategoryBase : DevelopmentCommandCategoryBase, IRoslynCommandCategory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandCategoryBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The name of the category.</param>
    /// <param name="description">The description of the category.</param>
    protected RoslynCommandCategoryBase(int id, string name, string description)
        : base(id, name, description)
    {
    }
}
