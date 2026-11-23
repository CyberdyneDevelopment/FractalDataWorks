using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A category's description: what the category is for, in the words of whoever made it (FDW-6815). Read by whoever holds <c>categories:read</c>, the permission the category routes ask. The category is the thing described, so the type has no owner: a category is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class CategoryDescriptionType : DescriptionTypeBase
{
    /// <summary>
    /// The type's name, which the category provider writes and reads its descriptions under. Not "Category": the type collection's generator emits one
    /// member per option, named as the option is, and a member named Category hides TypeOptionBase.Category (CS0108); the name is the table's, catalog.Category.
    /// </summary>
    public const string OptionName = "CatalogCategory";

    /// <summary>Initializes a new instance of the <see cref="CategoryDescriptionType"/> class.</summary>
    public CategoryDescriptionType()
        : base(OptionName, "categories:read", hasOwner: false)
    {
    }
}
