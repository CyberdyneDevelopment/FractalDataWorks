using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>FDW-5294: an abstract Graph data set states what it intends to read, and an intent names exactly what its kind says.</summary>
public class GraphDataSetIntentConfigurationTests
{
    private static readonly GraphDataSetIntentConfigurationValidator Validator = new();

    private static GraphDataSetIntentConfiguration StoreIntent() => new()
    {
        Id = Guid.NewGuid(),
        IntentKind = "DataStore",
        DataStoreName = "WeatherFeed"
    };

    private static GraphDataSetIntentConfiguration DataSetIntent() => new()
    {
        Id = Guid.NewGuid(),
        IntentKind = "DataSet",
        SourceDataSetId = Guid.NewGuid(),
        SourceDataSetName = "DailyWeather"
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void TheIntentKindsAreADataStoreAndADataSetAndEachSaysWhatItNames()
    {
        GraphIntentKinds.ByName("DataStore").NamesDataStore.ShouldBeTrue();
        GraphIntentKinds.ByName("DataStore").NamesDataSet.ShouldBeFalse();
        GraphIntentKinds.ByName("DataSet").NamesDataSet.ShouldBeTrue();
        GraphIntentKinds.ByName("DataSet").NamesDataStore.ShouldBeFalse();
        ReferenceEquals(GraphIntentKinds.ByName("Container"), GraphIntentKinds.NotFound).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void TheGeneratedMapperOfAnIntentCascadesNothing()
    {
        new GraphDataSetIntentConfigurationPocoMapper().CascadeChildren.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnIntentToADataStoreAndAnIntentToADataSetAreBothValidWithNoContainerMappingOrFilterChosen()
    {
        Validator.Validate(StoreIntent()).IsValid.ShouldBeTrue();
        Validator.Validate(DataSetIntent()).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnExternalDataStoreNeedsNoRegistrationToBeIntended()
    {
        var intent = StoreIntent();
        intent.DataStoreName = "SomeStoreNobodyHasRegistered";

        Validator.Validate(intent).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnIntentToADataStoreThatNamesNoStoreIsRefused()
    {
        var intent = StoreIntent();
        intent.DataStoreName = null;

        var errors = Validator.Validate(intent).Errors.Select(e => e.ErrorMessage).ToList();

        errors.ShouldContain("An intended source that names a data store must give its name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnIntentToADataStoreThatAlsoNamesADataSetIsRefused()
    {
        var intent = StoreIntent();
        intent.SourceDataSetId = Guid.NewGuid();
        intent.SourceDataSetName = "DailyWeather";

        var errors = Validator.Validate(intent).Errors.Select(e => e.ErrorMessage).ToList();

        errors.ShouldContain("An intended source that names a data store cannot also name a data set");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnIntentToADataSetThatNamesNoDataSetIsRefused()
    {
        var intent = DataSetIntent();
        intent.SourceDataSetId = null;
        intent.SourceDataSetName = null;

        var errors = Validator.Validate(intent).Errors.Select(e => e.ErrorMessage).ToList();

        errors.ShouldContain("An intended source that names a data set must give its id");
        errors.ShouldContain("An intended source that names a data set must give its name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnIntentToADataSetThatAlsoNamesADataStoreIsRefused()
    {
        var intent = DataSetIntent();
        intent.DataStoreName = "WeatherFeed";

        var errors = Validator.Validate(intent).Errors.Select(e => e.ErrorMessage).ToList();

        errors.ShouldContain("An intended source that names a data set cannot also name a data store");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnIntentOfAnUnknownKindOrNoKindIsRefused()
    {
        var unknown = StoreIntent();
        unknown.IntentKind = "Container";
        var none = StoreIntent();
        none.IntentKind = string.Empty;

        Validator.Validate(unknown).Errors.Select(e => e.ErrorMessage).ShouldContain("An intended source must name a GraphIntentKinds option");
        Validator.Validate(none).Errors.Select(e => e.ErrorMessage).ShouldContain("An intended source must say what it names");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AGraphDataSetCarriesItsIntentsAndHasNoneUntilSomeAreStated()
    {
        var graph = new GraphDataSetConfiguration();

        graph.Intents.Count.ShouldBe(0);

        graph.Intents.Add(StoreIntent());

        graph.Intents.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void ADataSetIsAbstractUntilAComponentFeedsAField()
    {
        var graph = new GraphDataSetConfiguration();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Temperature" });
        graph.Intents.Add(StoreIntent());

        graph.IsBound().ShouldBeFalse();

        graph.GraphFields[0].FedByComponent = "Join1";
        graph.GraphFields[0].FedByField = "Temperature";

        graph.IsBound().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void ADataSetWithAComponentIsBoundEvenBeforeAFieldIsFed()
    {
        var graph = new GraphDataSetConfiguration();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Temperature" });

        graph.IsBound().ShouldBeFalse();

        graph.Components.Add(new GraphDataSetComponentConfiguration { Id = Guid.NewGuid(), Name = "Source1" });

        graph.IsBound().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void BindingKeepsTheDataSetsIdAndTheSaveThatBindsCarriesNoIntents()
    {
        var graph = new GraphDataSetConfiguration { Id = Guid.NewGuid() };
        graph.Intents.Add(StoreIntent());
        var id = graph.Id;

        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Day", FedByComponent = "Source1", FedByField = "Day" });
        graph.Intents = [];

        graph.Id.ShouldBe(id);
        graph.IsBound().ShouldBeTrue();
        graph.Intents.Count.ShouldBe(0);
    }
}
