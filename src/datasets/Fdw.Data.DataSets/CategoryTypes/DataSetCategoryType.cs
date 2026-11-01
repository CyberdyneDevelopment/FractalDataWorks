using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A category the platform defines for organising data sets.</summary>
[TypeOption(typeof(CategoryTypes), "DataSet")]
public sealed class DataSetCategoryType : CategoryTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetCategoryType"/> class.</summary>
    public DataSetCategoryType() : base(DataSetCategoryConfiguration.ImplementationName, "A category the platform defines for organising data sets.")
    {
    }

    /// <inheritdoc />
    public override ICategoryImplementationConfiguration NewConfiguration() => new DataSetCategoryConfiguration();
}
