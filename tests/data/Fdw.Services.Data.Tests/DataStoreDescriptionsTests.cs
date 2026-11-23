using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A data store's description lives in the descriptions table and not on <c>data.DataStore</c> (FDW-6208): the provider describes the store in the unit of its
/// save, fills the text once for every set of composed stores and once for every page of domain records it reads, and removes it with the store. The writer and
/// reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataStoreDescriptionsTests
{
    private static readonly Guid OpsDb = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid NflDb = Guid.Parse("00000000-0000-0000-0000-0000000000d2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private DataStoreConfigurationProvider Stores()
        => new(
            NullLogger<DataStoreConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "data", "DataStore",
            new UnavailablePlacement(), new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict).Object, new Mock<ICrossTenantChangePolicy>(MockBehavior.Strict).Object,
            _writer.Object, _reader.Object);

    // The reader answers with the texts it holds, put on each store through the provider's own Id and fill delegates.
    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("DataStore", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<T>, Func<T, Guid>, Action<T, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    private void TheReaderFails<T>()
        => _reader
            .Setup(r => r.Fill("DataStore", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteReadAndPageObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var stores = Stores();

        stores.ShouldBeAssignableTo<IConfigurationWriteObserver<IDataStoreImplementationConfiguration>>();
        stores.ShouldBeAssignableTo<IConfigurationReadObserver<IDataStoreImplementationConfiguration>>();
        stores.ShouldBeAssignableTo<IConfigurationDomainReadObserver<DataStoreDomainConfiguration>>();
        stores.ShouldBeAssignableTo<IReferencedConfigurationProvider<IDataStoreImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDomainRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(DataStoreDomainConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("OpsDb — operational runtime data")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedStoreIsDescribedUnderTheDataStoreTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("DataStore", "Text", OpsDb, "OpsDb", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        // A store with no paths holds no children: each child type is kept in step with an empty list, which removes what the store no longer holds.
        _writer
            .Setup(w => w.DescribeOwned(It.IsAny<string>(), "Text", It.Is<DescriptionOwner>(owner => owner.Id == OpsDb), It.Is<IReadOnlyList<OwnedDescription>>(things => things.Count == 0), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Stores().Written(new DataStoreImplementationConfiguration { Id = OpsDb, Name = "OpsDb", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("DataStore", "Text", OpsDb, "OpsDb", "OpsDb — operational runtime data", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Stores().Written(
            new DataStoreImplementationConfiguration { Id = OpsDb, Name = "OpsDb", Description = "OpsDb — operational runtime data" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedStoreAndOneWithNoneStaysEmpty()
    {
        var stores = new List<IDataStoreImplementationConfiguration>
        {
            new DataStoreImplementationConfiguration { Id = OpsDb, Name = "OpsDb" },
            new DataStoreImplementationConfiguration { Id = NflDb, Name = "NflDb" },
        };
        TheReaderHolds<IDataStoreImplementationConfiguration>(new Dictionary<Guid, string> { [OpsDb] = "OpsDb — operational runtime data" });

        var read = await Stores().Read(stores, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        stores[0].Description.ShouldBe("OpsDb — operational runtime data");
        stores[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryStoreOfAListPage()
    {
        var page = new List<DataStoreDomainConfiguration> { new() { Id = OpsDb, Name = "OpsDb" }, new() { Id = NflDb, Name = "NflDb" } };
        TheReaderHolds<DataStoreDomainConfiguration>(new Dictionary<Guid, string> { [NflDb] = "DataDb — application data" });

        var read = await Stores().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("DataDb — application data");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPageAndOfComposedStores()
    {
        TheReaderFails<DataStoreDomainConfiguration>();
        TheReaderFails<IDataStoreImplementationConfiguration>();

        var page = await Stores().Read([new DataStoreDomainConfiguration { Id = OpsDb, Name = "OpsDb" }], TestContext.Current.CancellationToken);
        var composed = await Stores().Read([new DataStoreImplementationConfiguration { Id = NflDb, Name = "NflDb" }], null, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        composed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadPassesTheCallersTokenToTheFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "DataStore", "Text", It.IsAny<IReadOnlyList<IDataStoreImplementationConfiguration>>(), It.IsAny<Func<IDataStoreImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IDataStoreImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.Fill(
                "DataStore", "Text", It.IsAny<IReadOnlyList<DataStoreDomainConfiguration>>(), It.IsAny<Func<DataStoreDomainConfiguration, Guid>>(),
                It.IsAny<Action<DataStoreDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Stores().Read([new DataStoreImplementationConfiguration { Id = OpsDb, Name = "OpsDb" }], null, cancelled.Token);
        await Stores().Read([new DataStoreDomainConfiguration { Id = OpsDb, Name = "OpsDb" }], cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAStoreRemovesItsDescriptionsAndThoseOfItsPathsContainersFieldsAndKeysInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("DataStore", OpsDb, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        foreach (var type in new[] { "DataPath", "DataContainer", "DataContainerField", "DataContainerKey" })
        {
            _writer.Setup(w => w.DeleteOwnedBy(type, OpsDb, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        (await Stores().RemoveReferences(OpsDb, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }

    // A store with one path, one container under it, and that container's field and key: each child has a description but the key, which has none.
    private static DataStoreImplementationConfiguration StoreWithChildren(Guid id, string name, int n)
        => new()
        {
            Id = id,
            Name = name,
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = Child(n, 1), Name = "dbo", Description = "the dbo schema",
                    Containers =
                    [
                        new DataContainerConfiguration
                        {
                            Id = Child(n, 2), Name = "Orders", Description = "the orders table",
                            Fields = [new DataContainerFieldConfiguration { Id = Child(n, 3), Name = "OrderId", Description = "the order's key" }],
                            Keys = [new DataContainerKeyConfiguration { Id = Child(n, 4), Name = "PK_Orders" }],
                        },
                    ],
                },
            ],
        };

    private static Guid Child(int store, int child) => new(store, (short)child, 0, [0, 0, 0, 0, 0, 0, 0, 9]);

    private void TheReaderOwns(string type, Dictionary<Guid, string> texts, params Guid[] owners)
        => _reader
            .Setup(r => r.ReadOwnedBy(type, "Text", It.Is<IReadOnlyCollection<Guid>>(ids => ids.OrderBy(id => id).SequenceEqual(owners.OrderBy(id => id))), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(texts.ToDictionary(
                pair => pair.Key, pair => new DescriptionEntry(new DescriptionKey(type, "Text", pair.Key, "en-US"), "thing", new DescriptionOwner(owners[0], "store"), pair.Value))));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheChildrensMappersHaveNoDescriptionSoNoReadSelectsTheColumnsAndNoSaveWritesThem()
    {
        foreach (var record in new[] { "DataPathConfiguration", "DataContainerConfiguration", "DataContainerFieldConfiguration", "DataContainerKeyConfiguration" })
        {
            var mapper = PocoMapperCollection.ByName(record);
            mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound, record);
            mapper.GetPropertyNames().ShouldNotContain("Description", record);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedStoreKeepsItsPathsContainersFieldsAndKeysInStepWithTheStoreAsOwner()
    {
        _writer
            .Setup(w => w.Describe("DataStore", "Text", OpsDb, "OpsDb", null, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var handed = new Dictionary<string, List<OwnedDescription>>(StringComparer.Ordinal);
        _writer
            .Setup(w => w.DescribeOwned(It.IsAny<string>(), "Text", It.Is<DescriptionOwner>(owner => owner.Id == OpsDb && owner.Name == "OpsDb"), It.IsAny<IReadOnlyList<OwnedDescription>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, DescriptionOwner, IReadOnlyList<OwnedDescription>, ConfigurationWriteScope, CancellationToken>((type, _, _, things, _, _) => handed[type] = [.. things])
            .ReturnsAsync(GenericResult.Success());

        var written = await Stores().Written(StoreWithChildren(OpsDb, "OpsDb", 1), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        handed.Keys.Order(StringComparer.Ordinal).ShouldBe(["DataContainer", "DataContainerField", "DataContainerKey", "DataPath"]);
        handed["DataPath"].ShouldBe([new OwnedDescription(Child(1, 1), "dbo", "the dbo schema")]);
        handed["DataContainer"].ShouldBe([new OwnedDescription(Child(1, 2), "Orders", "the orders table")]);
        handed["DataContainerField"].ShouldBe([new OwnedDescription(Child(1, 3), "OrderId", "the order's key")]);
        handed["DataContainerKey"].ShouldBe([new OwnedDescription(Child(1, 4), "PK_Orders", null)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadThatComposedChildrenFillsEachChildTypeWithOneReadForTheWholeSet()
    {
        var stores = new List<IDataStoreImplementationConfiguration> { StoreWithChildren(OpsDb, "OpsDb", 1), StoreWithChildren(NflDb, "NflDb", 2) };
        TheReaderHolds<IDataStoreImplementationConfiguration>(new Dictionary<Guid, string>());
        TheReaderOwns("DataPath", new() { [Child(1, 1)] = "operational schema" }, OpsDb, NflDb);
        TheReaderOwns("DataContainer", new() { [Child(2, 2)] = "the players" }, OpsDb, NflDb);
        TheReaderOwns("DataContainerField", new() { [Child(1, 3)] = "the run's key", [Child(2, 3)] = "the player's key" }, OpsDb, NflDb);
        TheReaderOwns("DataContainerKey", new() { [Child(2, 4)] = "one row per player" }, OpsDb, NflDb);

        var read = await Stores().Read(stores, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var ops = (DataStoreImplementationConfiguration)stores[0];
        var nfl = (DataStoreImplementationConfiguration)stores[1];
        ops.Paths[0].Description.ShouldBe("operational schema");
        nfl.Paths[0].Description.ShouldBe("the dbo schema");
        nfl.Paths[0].Containers[0].Description.ShouldBe("the players");
        ops.Paths[0].Containers[0].Fields[0].Description.ShouldBe("the run's key");
        nfl.Paths[0].Containers[0].Fields[0].Description.ShouldBe("the player's key");
        nfl.Paths[0].Containers[0].Keys[0].Description.ShouldBe("one row per player");
        ops.Paths[0].Containers[0].Keys[0].Description.ShouldBeNull();
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadOfTheRecordAloneReadsNoChildDescriptions()
    {
        TheReaderHolds<IDataStoreImplementationConfiguration>(new Dictionary<Guid, string>());

        var read = await Stores().Read([new DataStoreImplementationConfiguration { Id = OpsDb, Name = "OpsDb" }], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChildReadOfAContainersFieldsFillsThemFromTheDescriptionsTheStoreOwnsAndReadsNothingElse()
    {
        // FDW-6777: a container's fields and keys are read with Get<DataContainerConfiguration, DataContainerFieldConfiguration>, which composes no store.
        TheReaderOwns("DataContainerField", new() { [Child(1, 3)] = "the order's key" }, OpsDb);
        var fields = new List<DataContainerFieldConfiguration> { new() { Id = Child(1, 3), Name = "OrderId" }, new() { Id = Child(1, 5), Name = "Placed" } };

        var read = await Stores().ReadChildren(OpsDb, fields, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        fields[0].Description.ShouldBe("the order's key");
        fields[1].Description.ShouldBeNull();
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfAChildTypeFailsTheRead()
    {
        TheReaderHolds<IDataStoreImplementationConfiguration>(new Dictionary<Guid, string>());
        _reader
            .Setup(r => r.ReadOwnedBy("DataPath", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Stores().Read([StoreWithChildren(OpsDb, "OpsDb", 1)], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }
}
