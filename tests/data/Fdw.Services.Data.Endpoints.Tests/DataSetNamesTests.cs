using System;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6117, FDW-6129: a data set's display name and abbreviation are stated on create and update. A request that leaves one out keeps the stored
/// one, an empty one clears it, and one longer than its column is refused with a code of its own before anything is written.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public sealed class DataSetNamesTests
{
    [Fact]
    public void ANameAtItsLimitIsAccepted()
    {
        DataSetNames.Check(new string('a', 200), new string('b', 20), "Orders", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ADisplayNameOfMoreThanTwoHundredCharactersIsRefusedWithItsOwnCode()
    {
        var checkedNames = DataSetNames.Check(new string('a', 201), null, "Orders", NullLogger.Instance);

        checkedNames.IsSuccess.ShouldBeFalse();
        checkedNames.Code!.Code.ShouldBe("DATASETS-20031");
        checkedNames.Details!.GetValue<string>("name").ShouldBe("Orders");
    }

    [Fact]
    public void AnAbbreviationOfMoreThanTwentyCharactersIsRefusedWithItsOwnCode()
    {
        var checkedNames = DataSetNames.Check(null, new string('b', 21), "Orders", NullLogger.Instance);

        checkedNames.IsSuccess.ShouldBeFalse();
        checkedNames.Code!.Code.ShouldBe("DATASETS-20032");
        checkedNames.Details!.GetValue<string>("name").ShouldBe("Orders");
    }

    [Fact]
    public void NamesLeftOutAreAccepted()
    {
        DataSetNames.Check(null, null, "Orders", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void NamesStatedAreSetOnTheDataSet()
    {
        var config = new SimpleDataSetConfiguration { Name = "Orders" };

        DataSetNames.Set(config, "Customer orders", "ORD");

        config.DisplayName.ShouldBe("Customer orders");
        config.Abbreviation.ShouldBe("ORD");
    }

    [Fact]
    public void NamesLeftOutKeepTheStoredOnes()
    {
        var config = new CompoundDataSetConfiguration { Name = "Orders", DisplayName = "Customer orders", Abbreviation = "ORD" };

        DataSetNames.Set(config, null, null);

        config.DisplayName.ShouldBe("Customer orders");
        config.Abbreviation.ShouldBe("ORD");
    }

    [Fact]
    public void EmptyNamesClearTheStoredOnes()
    {
        var config = new FederatedDataSetConfiguration { Name = "Orders", DisplayName = "Customer orders", Abbreviation = "ORD" };

        DataSetNames.Set(config, string.Empty, string.Empty);

        config.DisplayName.ShouldBeNull();
        config.Abbreviation.ShouldBeNull();
    }

    [Fact]
    public void OneNameStatedLeavesTheOtherAlone()
    {
        var config = new SimpleDataSetConfiguration { Name = "Orders", DisplayName = "Customer orders", Abbreviation = "ORD" };

        DataSetNames.Set(config, null, "OR");

        config.DisplayName.ShouldBe("Customer orders");
        config.Abbreviation.ShouldBe("OR");
    }

    [Fact]
    public void ANewDataSetThatStatesNoNamesHasNone()
    {
        var config = new SimpleDataSetConfiguration { Name = "Orders" };

        DataSetNames.Set(config, null, null);

        config.DisplayName.ShouldBeNull();
        config.Abbreviation.ShouldBeNull();
    }

    [Fact]
    public void ACreateThatStatesTooLongANameIsRefusedBeforeAnythingIsBuilt()
    {
        var request = new CreateDataSetRequest { Name = "Orders", DisplayName = new string('a', 201) };

        var built = DataSetConfigurationFactory.Create(request, "Simple", null, Guid.NewGuid(), new DataSetRequestValues("Orders", NullLogger.Instance), NullLogger.Instance);

        built.IsSuccess.ShouldBeFalse();
        built.Code!.Code.ShouldBe("DATASETS-20031");
    }

    [Fact]
    public void ACreateThatStatesTooLongAnAbbreviationIsRefusedBeforeAnythingIsBuilt()
    {
        var request = new CreateDataSetRequest { Name = "Orders", Abbreviation = new string('b', 21) };

        var built = DataSetConfigurationFactory.Create(request, "Simple", null, Guid.NewGuid(), new DataSetRequestValues("Orders", NullLogger.Instance), NullLogger.Instance);

        built.IsSuccess.ShouldBeFalse();
        built.Code!.Code.ShouldBe("DATASETS-20032");
    }
}
