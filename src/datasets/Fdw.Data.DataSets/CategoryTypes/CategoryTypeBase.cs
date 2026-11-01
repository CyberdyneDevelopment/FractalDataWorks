using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The base of the types of category.</summary>
public abstract class CategoryTypeBase : TypeOptionBase<CategoryTypeBase>, ICategoryType
{
    /// <summary>Initializes a new instance of the <see cref="CategoryTypeBase"/> class.</summary>
    /// <param name="name">The type's name: the implementation name its categories are stored under.</param>
    /// <param name="description">What a category of this type is for.</param>
    protected CategoryTypeBase(string name, string description)
        : base(name, name, description, "CategoryTypes")
    {
    }

    /// <inheritdoc />
    public abstract ICategoryImplementationConfiguration NewConfiguration();
}
