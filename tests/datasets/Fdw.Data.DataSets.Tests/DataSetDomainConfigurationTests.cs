using System.Linq;
using Fdw.Configuration;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// FDW-6129: a data set's display name and abbreviation sit on its domain row beside its name, as the fields a list of data sets filters on and
/// shows. The domain provider stamps them onto the data set it reads and takes them from the one it saves, so every data set type has to carry
/// both properties, or a read or a save of that type fails with ListFieldNotOnImplementation.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Configuration")]
public class DataSetDomainConfigurationTests
{
    private static readonly string[] ListFields = ["DisplayName", "Abbreviation"];

    [Fact]
    public void TheDomainRecordHoldsTheTwoNamesBesideTheNameEveryDomainHas()
    {
        var record = PocoMapperCollection.ByName(nameof(DataSetDomainConfiguration));
        var plain = PocoMapperCollection.ByName(nameof(DomainConfiguration));

        record.GetPropertyNames().Except(plain.GetPropertyNames()).ShouldBe(ListFields, ignoreOrder: true);
    }

    [Theory]
    [InlineData(nameof(SimpleDataSetConfiguration))]
    [InlineData(nameof(CompoundDataSetConfiguration))]
    [InlineData(nameof(FederatedDataSetConfiguration))]
    [InlineData(nameof(GraphDataSetConfiguration))]
    [InlineData(nameof(VariationDataSetConfiguration))]
    public void EveryDataSetTypeCarriesBothNames(string typeName)
    {
        ListFields.Except(PocoMapperCollection.ByName(typeName).GetPropertyNames()).ShouldBeEmpty();
    }

    [Fact]
    public void ADataSetRecordProviderReadsAPageOfRecordsThatHoldTheNames()
    {
        typeof(IDataSetRecordProvider).GetInterfaces().ShouldContain(typeof(IDomainRecordProvider<DataSetDomainConfiguration>));
    }

    [Fact]
    public void TheDataSetConfigurationProviderKeepsReadingAPageOfPlainDomainRecords()
    {
        // Every caller that reads the data sets as plain domain records (the map, the lookup, a variation's base) keeps doing so.
        typeof(IDataSetConfigurationProvider).GetInterfaces().ShouldContain(typeof(IDomainRecordProvider<DomainConfiguration>));
        typeof(IDataSetConfigurationProvider).GetInterfaces().ShouldNotContain(typeof(IDomainRecordProvider<DataSetDomainConfiguration>));
    }

    [Fact]
    public void ANewDomainRecordHasNoNames()
    {
        var record = new DataSetDomainConfiguration();

        record.DisplayName.ShouldBeNull();
        record.Abbreviation.ShouldBeNull();
    }
}
