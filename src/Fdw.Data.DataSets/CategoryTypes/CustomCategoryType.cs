using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A category an administrator creates at runtime, for whatever they choose to organise.</summary>
[TypeOption(typeof(CategoryTypes), "Custom")]
public sealed class CustomCategoryType : CategoryTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CustomCategoryType"/> class.</summary>
    public CustomCategoryType() : base(CustomCategoryConfiguration.ImplementationName, "A category an administrator creates at runtime, for whatever they choose to organise.")
    {
    }

    /// <inheritdoc />
    public override ICategoryImplementationConfiguration NewConfiguration() => new CustomCategoryConfiguration();
}
