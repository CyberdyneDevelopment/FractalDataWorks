using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// FDW-3873: the generated mapper of a variation makes no child table of its lists. A variation varies another data set and stores none of the
/// rows of Fields, Sources, Filters or KeyFields; a child the mapper cascades is read by a foreign key from its table to the implementation's, so
/// a cascaded list of a variation refuses the read of the whole record ("Container 'DataSetField' declares no Foreign key to
/// 'VariationDataSetConfiguration'"), which failed the lineage, the map and the data route on dev.
/// </summary>
public class VariationDataSetConfigurationMapperTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheGeneratedMapperOfAVariationCascadesNoChildTable()
    {
        new VariationDataSetConfigurationPocoMapper().CascadeChildren.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheListsAVariationDoesNotStoreAreStillThereForTheInterfaceItImplements()
    {
        var variation = new VariationDataSetConfiguration();
        IDataSetImplementationConfiguration asDataSet = variation;

        asDataSet.Fields.ShouldBeEmpty();
        asDataSet.Sources.ShouldBeEmpty();
        asDataSet.Filters.ShouldBeEmpty();
        asDataSet.KeyFields.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheOtherKindsStillCascadeTheirChildTablesSoTheTestAboveMeansSomething()
    {
        new SimpleDataSetConfigurationPocoMapper().CascadeChildren.Any().ShouldBeTrue();
    }
}
