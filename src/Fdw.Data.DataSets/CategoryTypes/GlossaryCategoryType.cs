using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A category for organising glossary terms.</summary>
[TypeOption(typeof(CategoryTypes), "Glossary")]
public sealed class GlossaryCategoryType : CategoryTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryCategoryType"/> class.</summary>
    public GlossaryCategoryType() : base(GlossaryCategoryConfiguration.ImplementationName, "A category for organising glossary terms.")
    {
    }

    /// <inheritdoc />
    public override ICategoryImplementationConfiguration NewConfiguration() => new GlossaryCategoryConfiguration();
}
