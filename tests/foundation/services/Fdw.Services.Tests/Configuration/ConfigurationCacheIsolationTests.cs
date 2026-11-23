using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Verifies repeated domain reads cannot replace the cached implementation identity used by child joins, and the one rule behind it (FDW-7326): a row the gateway
/// cached is never changed by a read. That holds for implementation records, for the sets read of a page and for the domain rows of a domain page, whose read observers
/// write the description onto the rows they are given. It also verifies that a record read without the members another domain supplies is for reading and is never saved.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfigurationCacheIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task RepeatedDomainReadsPreserveDatastorePaths(bool cacheRows)
    {
        // Arrange — a store with one path holding one container, linked by physical RowId FKs, exactly
        // as the seeded AuthDb → auth → RevokedAccessToken rows are in ConfigurationDb's data.* tables.
        // RowId is never set: it is DB-managed and absent from every config POCO; only the durable Id is.
        PocoMapperCollection.RegisterMember(new DataStoreImplementationConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new global::Fdw.Services.Data.Configuration.DataStoreDomainConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new DataPathConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new DataContainerConfigurationPocoMapper());
        var store = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "AuthDb" };
        var path = new DataPathConfiguration { Id = Guid.NewGuid(), Name = "auth" };
        var container = new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "RevokedAccessToken" };

        // FDW-4254: a read by durable id is the set read of one, so the store answers with rows as columns, a physical RowId on each, and a
        // child row names the row it hangs from by that RowId. The domain row, the implementation row, the path and the container.
        var gateway = new ComposingReadGateway(store, cacheRows);
        gateway.SeedRows("DataStore", Row(1, ("Id", store.Id), ("Name", "AuthDb")));
        gateway.SeedRows("DataStoreConfiguration", Row(11, ("DataStoreRowId", 1), ("Id", Guid.NewGuid()), ("Name", "AuthDb")));
        gateway.SeedRows("DataPath", Row(21, ("DataStoreConfigurationRowId", 11), ("Id", path.Id), ("Name", "auth")));
        gateway.SeedRows("DataContainer", Row(31, ("DataPathRowId", 21), ("Id", container.Id), ("Name", "RevokedAccessToken")));

        // Why the named provider and not the base: the base is abstract now, and the cascade under test
        // lives in ImplementationProviderBase, which DataStoreImplementationConfigurationProvider closes.
        var provider = new DataStoreImplementationConfigurationProvider(
            NullLogger<DataStoreImplementationConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "ConfigurationDb", "data", "DataStoreConfiguration");

        // Act -- an implementation provider reads by the owning domain row's durable id, not by name.
        var domain = new DataStoreConfigurationProvider(NullLogger<DataStoreConfigurationProvider>.Instance, GatewayProviderFor(gateway), "ConfigurationDb", "data", "DataStore", new UnavailablePlacement(), Moq.Mock.Of<Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContextAccessor>(), Moq.Mock.Of<Fdw.Services.Multitenancy.Abstractions.ICrossTenantChangePolicy>(),
            new Moq.Mock<Fdw.Services.Descriptions.IDescriptionWriter>(Moq.MockBehavior.Strict).Object, NoDescriptions());
        domain.Register("MsSql", provider);
        var implementationId = store.Id;
        var first = await domain.Get("AuthDb", TestContext.Current.CancellationToken);
        first.IsSuccess.ShouldBeTrue(first.CurrentMessage?.ToString());
        first.Value!.Paths.Count.ShouldBe(1);
        store.Id.ShouldBe(implementationId);
        first.Value.ShouldNotBeSameAs(store);
        first.Value.LastDiscoveredAt.ShouldBeNull();
        var result = await domain.Get("AuthDb", TestContext.Current.CancellationToken);

        // Assert — the FULL aggregate composed: DataStore → Paths(1) → Containers(1).
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Paths.ShouldHaveSingleItem();
        result.Value.Paths[0].Name.ShouldBe("auth");
        result.Value.Paths[0].Containers.ShouldHaveSingleItem();
        result.Value.Paths[0].Containers[0].Name.ShouldBe("RevokedAccessToken");

        // Assert — the cascade read each owner level's children by the physical keys of the rows above (one read of each table,
        // however many rows), and the second read built its own records from the rows, not the rows themselves.
        gateway.RowReads.ShouldContain("DataPath");
        gateway.RowReads.ShouldContain("DataContainer");
        gateway.RowReads.Count(read => read == "DataPath").ShouldBe(2);
    }


    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task TheDomainPageFillsACopyOfTheRowTheGatewayCachedAndNeverTheRowItself()
    {
        // Arrange -- the gateway hands the same domain row to every caller of the page, as its cache does.
        var cached = new global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration { Id = Guid.NewGuid(), Name = "AuthDb", Implementation = "MsSql" };
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, cached);

        // Act
        var page = await domain.Get(new DomainQuery(null, null, 0, 10, false), TestContext.Current.CancellationToken);

        // Assert -- the page shows the text, the cached row never does.
        page.IsSuccess.ShouldBeTrue(page.CurrentMessage?.ToString());
        var shown = await Rows(page.Value!);
        shown.ShouldHaveSingleItem().Description.ShouldBe("the stored text");
        shown[0].ShouldNotBeSameAs(cached);
        shown[0].Id.ShouldBe(cached.Id);
        shown[0].Name.ShouldBe("AuthDb");
        cached.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task ATextRemovedAfterADomainPageReadIsNotShownByTheNextPageRead()
    {
        // Arrange -- a text removed by a write that does not go through the domain's own save (a seed or a migration against a live database) leaves the cached row
        // as it was; a page that filled the cached row itself would show the old text for as long as the entry lives. No route does this today: the test guards the path.
        var cached = new global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration { Id = Guid.NewGuid(), Name = "AuthDb", Implementation = "MsSql" };
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, cached);
        var first = await Rows((await domain.Get(new DomainQuery(null, null, 0, 10, false), TestContext.Current.CancellationToken)).Value!);
        first.ShouldHaveSingleItem().Description.ShouldBe("the stored text");

        // Act
        descriptions.Text = null;
        var second = await Rows((await domain.Get(new DomainQuery(null, null, 0, 10, false), TestContext.Current.CancellationToken)).Value!);

        // Assert
        second.ShouldHaveSingleItem().Description.ShouldBeNull();
        second[0].ShouldNotBeSameAs(first[0]);
        first[0].Description.ShouldBe("the stored text");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task AWholeReadAndALeanReadOfOneMemberDoNotShareTheTextInEitherOrder(bool wholeFirst)
    {
        // Arrange
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, null);

        // Act
        var first = await domain.Get("AuthDb", ConfigurationLevels.WholeAggregate, wholeFirst ? ConfigurationReadOptions.Whole : ConfigurationReadOptions.WithoutFilledMembers, TestContext.Current.CancellationToken);
        var second = await domain.Get("AuthDb", ConfigurationLevels.WholeAggregate, wholeFirst ? ConfigurationReadOptions.WithoutFilledMembers : ConfigurationReadOptions.Whole, TestContext.Current.CancellationToken);

        // Assert -- each read is what it stated, whatever the other did before it.
        first.IsSuccess.ShouldBeTrue(first.CurrentMessage?.ToString());
        second.IsSuccess.ShouldBeTrue(second.CurrentMessage?.ToString());
        (wholeFirst ? first : second).Value!.Description.ShouldBe("the stored text");
        (wholeFirst ? second : first).Value!.Description.ShouldBeNull();
        descriptions.Fills.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task ATextRemovedAfterAWholeReadIsNotShownByTheNextWholeRead()
    {
        // Arrange
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, null);
        (await domain.Get("AuthDb", TestContext.Current.CancellationToken)).Value!.Description.ShouldBe("the stored text");

        // Act
        descriptions.Text = null;
        var next = await domain.Get("AuthDb", TestContext.Current.CancellationToken);

        // Assert
        next.Value!.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task ALeanReadRunsNoFillObserverForAnyRead()
    {
        // Arrange
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, null);
        var lean = ConfigurationReadOptions.WithoutFilledMembers;
        var token = TestContext.Current.CancellationToken;

        // Act -- every read that can state it: one by name, by id, the whole list and the flat list.
        var byName = await domain.Get("AuthDb", ConfigurationLevels.WholeAggregate, lean, token);
        var byId = await domain.Get(byName.Value!.Id, ConfigurationLevels.WholeAggregate, lean, token);
        var all = await domain.Get(ConfigurationLevels.WholeAggregate, lean, token);
        var flat = await domain.GetFlat(ConfigurationLevels.WholeAggregate, lean, token);

        // Assert
        byName.Value!.Description.ShouldBeNull();
        byId.Value!.Description.ShouldBeNull();
        all.Value!.ShouldAllBe(member => member.Description == null);
        flat.Value!.ShouldAllBe(member => member.Description == null);
        descriptions.Fills.ShouldBe(0);
    }

    [Theory]
    [InlineData("Update")]
    [InlineData("Save")]
    [InlineData("UpdateInUnit")]
    [InlineData("SaveInUnit")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task ARecordReadWithoutTheFilledMembersIsRefusedByEverySaveAndTheStoredTextIsKept(string write)
    {
        // Arrange -- a lean read leaves the description empty, and the write observer would take the empty member for "cleared" and remove the stored text.
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, null);
        var lean = await domain.Get("AuthDb", ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, TestContext.Current.CancellationToken);
        var record = lean.Value!;

        // Act -- the unit overloads refuse before they touch the unit, so none is opened here.
        var refused = write switch
        {
            "Update" => await domain.Update(record, "DataStore", "MsSql", "AuthDb", TestContext.Current.CancellationToken),
            "Save" => await domain.Save(record, "DataStore", "MsSql", "AuthDb", TestContext.Current.CancellationToken),
            "UpdateInUnit" => await domain.Update(record, "DataStore", "MsSql", "AuthDb", null!, TestContext.Current.CancellationToken),
            _ => await domain.Save(record, "DataStore", "MsSql", "AuthDb", null!, TestContext.Current.CancellationToken),
        };

        // Assert -- refused with the message that says what to do, before any write: the strict writer would throw at the first call, and nothing was opened.
        refused.IsFailure.ShouldBeTrue();
        refused.Messages[^1].Code.ShouldEndWith("-21082");
        refused.CurrentMessage!.ToString().ShouldContain("read the record whole before saving it");
        descriptions.Writes.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task ARowOfALeanPageIsRefusedByUpdateAndTheStoredTextIsKept()
    {
        // Arrange -- the page read of FDW-6124 states WithoutFilledMembers; the members it hands out are for reading.
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, null);
        var page = await domain.Get(0, 10, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, TestContext.Current.CancellationToken);
        page.IsSuccess.ShouldBeTrue(page.CurrentMessage?.ToString());
        var members = new List<IDataStoreImplementationConfiguration>();
        await foreach (var member in page.Value!.Items) members.Add(member);
        members.ShouldHaveSingleItem().Description.ShouldBeNull();

        // Act
        var refused = await domain.Update(members[0], "DataStore", "MsSql", "AuthDb", TestContext.Current.CancellationToken);

        // Assert
        refused.IsFailure.ShouldBeTrue();
        refused.Messages[^1].Code.ShouldEndWith("-21082");
        descriptions.Writes.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task ARecordReadWholeIsNotRefusedAsLeanAndKeepsItsText()
    {
        // Arrange -- the lean record of one read does not mark the whole record of another: the registry is per instance, so the save goes on past the refusal.
        var descriptions = new HeldDescriptions { Text = "the stored text" };
        var (domain, _) = DataStoreWith(descriptions, null);
        var lean = await domain.Get("AuthDb", ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, TestContext.Current.CancellationToken);
        var whole = await domain.Get("AuthDb", TestContext.Current.CancellationToken);
        whole.Value!.Description.ShouldBe("the stored text");
        whole.Value.ShouldNotBeSameAs(lean.Value);

        // Act -- this gateway opens no unit of work, which is the first thing a save that was not refused asks for, so it ends in that failure or in a thrown one.
        IGenericResult? answer = null;
        try
        {
            answer = await domain.Update(whole.Value, "DataStore", "MsSql", "AuthDb", TestContext.Current.CancellationToken);
        }
        catch (NotSupportedException)
        {
            // The double has no unit of work: the save got past the refusal to the place that asks for one.
        }

        // Assert
        (answer is null || answer.Messages.All(message => !(message.Code ?? string.Empty).EndsWith("-21082", StringComparison.Ordinal))).ShouldBeTrue();
    }

    // The data stores domain over a gateway that holds one store with a path and a container, as the first test builds it; the descriptions are the ones the test holds.
    private static (DataStoreConfigurationProvider Domain, ComposingReadGateway Gateway) DataStoreWith(
        HeldDescriptions descriptions, global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration? sharedDomainRow)
    {
        PocoMapperCollection.RegisterMember(new DataStoreImplementationConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new global::Fdw.Services.Data.Configuration.DataStoreDomainConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new DataPathConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new DataContainerConfigurationPocoMapper());
        var store = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "AuthDb" };
        var gateway = new ComposingReadGateway(store, cacheRows: true) { SharedDomainRow = sharedDomainRow };
        gateway.SeedRows("DataStore", Row(1, ("Id", store.Id), ("Name", "AuthDb")));
        gateway.SeedRows("DataStoreConfiguration", Row(11, ("DataStoreRowId", 1), ("Id", Guid.NewGuid()), ("Name", "AuthDb")));
        gateway.SeedRows("DataPath", Row(21, ("DataStoreConfigurationRowId", 11), ("Id", Guid.NewGuid()), ("Name", "auth")));
        gateway.SeedRows("DataContainer", Row(31, ("DataPathRowId", 21), ("Id", Guid.NewGuid()), ("Name", "RevokedAccessToken")));

        var implementation = new DataStoreImplementationConfigurationProvider(
            NullLogger<DataStoreImplementationConfigurationProvider>.Instance, GatewayProviderFor(gateway), "ConfigurationDb", "data", "DataStoreConfiguration");
        var domain = new DataStoreConfigurationProvider(NullLogger<DataStoreConfigurationProvider>.Instance, GatewayProviderFor(gateway), "ConfigurationDb", "data", "DataStore", new UnavailablePlacement(),
            Moq.Mock.Of<Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContextAccessor>(), Moq.Mock.Of<Fdw.Services.Multitenancy.Abstractions.ICrossTenantChangePolicy>(),
            descriptions.Writer, descriptions.Reader);
        domain.Register("MsSql", implementation);
        return (domain, gateway);
    }

    private static async Task<List<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>> Rows(
        ConfigurationPage<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration> page)
    {
        var rows = new List<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>();
        await foreach (var row in page.Items) rows.Add(row);
        return rows;
    }

    // The descriptions a test holds for the data stores: the text the reader fills (none when it is null) and how often each side was asked. The writer is strict, so a write
    // through it, which would remove or replace a stored text, is a thrown failure as well as a count.
    private sealed class HeldDescriptions
    {
        private readonly Moq.Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader> _reader = new(Moq.MockBehavior.Strict);
        private readonly Moq.Mock<Fdw.Services.Descriptions.IDescriptionWriter> _writer = new(Moq.MockBehavior.Strict);

        public HeldDescriptions()
        {
            _reader
                .Setup(r => r.Fill("DataStore", "Text", Moq.It.IsAny<IReadOnlyList<IDataStoreImplementationConfiguration>>(),
                    Moq.It.IsAny<Func<IDataStoreImplementationConfiguration, Guid>>(), Moq.It.IsAny<Action<IDataStoreImplementationConfiguration, string>>(), Moq.It.IsAny<CancellationToken>()))
                .Callback<string, string, IReadOnlyList<IDataStoreImplementationConfiguration>, Func<IDataStoreImplementationConfiguration, Guid>, Action<IDataStoreImplementationConfiguration, string>, CancellationToken>(
                    (_, _, things, _, fill, _) =>
                    {
                        Fills++;
                        if (Text is { } text)
                            foreach (var thing in things) fill(thing, text);
                    })
                .Returns(Task.FromResult<IGenericResult>(GenericResult.Success()));
            _reader
                .Setup(r => r.Fill("DataStore", "Text", Moq.It.IsAny<IReadOnlyList<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>>(),
                    Moq.It.IsAny<Func<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration, Guid>>(),
                    Moq.It.IsAny<Action<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration, string>>(), Moq.It.IsAny<CancellationToken>()))
                .Callback<string, string, IReadOnlyList<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>,
                    Func<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration, Guid>, Action<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration, string>, CancellationToken>(
                    (_, _, things, _, fill, _) =>
                    {
                        if (Text is { } text)
                            foreach (var thing in things) fill(thing, text);
                    })
                .Returns(Task.FromResult<IGenericResult>(GenericResult.Success()));
            // The store read has a path and a container: their descriptions are read for the set too, and none is held.
            _reader
                .Setup(r => r.ReadOwnedBy(Moq.It.IsAny<string>(), "Text", Moq.It.IsAny<IReadOnlyCollection<Guid>>(), Moq.It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IGenericResult<IReadOnlyDictionary<Guid, Fdw.Services.Descriptions.Abstractions.DescriptionEntry>>>(
                    GenericResult<IReadOnlyDictionary<Guid, Fdw.Services.Descriptions.Abstractions.DescriptionEntry>>.Success(new Dictionary<Guid, Fdw.Services.Descriptions.Abstractions.DescriptionEntry>())));
        }

        /// <summary>Gets or sets the text the reader fills onto every thing it is given, or none.</summary>
        public string? Text { get; set; }

        /// <summary>Gets how many times the reader filled the implementation records of a read.</summary>
        public int Fills { get; private set; }

        /// <summary>Gets how many writes went to the writer; it is strict, so the first one throws.</summary>
        public int Writes => _writer.Invocations.Count;

        public Fdw.Services.Descriptions.Abstractions.IDescriptionReader Reader => _reader.Object;

        public Fdw.Services.Descriptions.IDescriptionWriter Writer => _writer.Object;
    }

    // The data store provider fills each store's description on a read (FDW-6208); this test is about the cache, so the reader states its answer: no text held.
    private static Fdw.Services.Descriptions.Abstractions.IDescriptionReader NoDescriptions()
    {
        var reader = new Moq.Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>(Moq.MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("DataStore", "Text", Moq.It.IsAny<IReadOnlyList<IDataStoreImplementationConfiguration>>(),
                Moq.It.IsAny<Func<IDataStoreImplementationConfiguration, Guid>>(), Moq.It.IsAny<Action<IDataStoreImplementationConfiguration, string>>(), Moq.It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGenericResult>(GenericResult.Success()));
        // The store read here has a path and a container: their descriptions are read for the set too (FDW-6589), and none is held.
        reader
            .Setup(r => r.ReadOwnedBy(Moq.It.IsAny<string>(), "Text", Moq.It.IsAny<IReadOnlyCollection<Guid>>(), Moq.It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGenericResult<IReadOnlyDictionary<Guid, Fdw.Services.Descriptions.Abstractions.DescriptionEntry>>>(
                GenericResult<IReadOnlyDictionary<Guid, Fdw.Services.Descriptions.Abstractions.DescriptionEntry>>.Success(new Dictionary<Guid, Fdw.Services.Descriptions.Abstractions.DescriptionEntry>())));
        return reader.Object;
    }

    private static Dictionary<string, object?> Row(int rowId, params (string Column, object? Value)[] columns)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal) { ["RowId"] = rowId };
        foreach (var (column, value) in columns)
            row[column] = value;

        return row;
    }

    /// <summary>
    /// Gateway double: returns the store header on the by-name read and seeded child rows on the by-type
    /// child read, recording the child <see cref="Type"/> each child query requested so the test can assert
    /// the cascade ran a JOIN-based read per owner level. <see cref="DataStores"/> carries the schema tree
    /// with each owner container's Physical (RowId) + Logical (Id) keys — the column names the cascade
    /// resolves to build the JOIN. DataPath rows are returned only when the generated JOIN filters by the owning domain row's Id,
    /// which is the Id the implementation row carries.
    /// </summary>
    private sealed class ComposingReadGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly bool _cacheRows;
        private readonly Guid _originalId;
        private readonly DataStoreImplementationConfiguration _header;
        private readonly Dictionary<Type, List<object>> _childrenByType = new();
        private readonly IReadOnlyList<IDataStore> _stores;

        public ComposingReadGateway(DataStoreImplementationConfiguration header, bool cacheRows)
        {
            _header = header; _originalId = header.Id; _cacheRows = cacheRows;
            _stores = [BuildTree()];
        }

        public List<Type> ChildReads { get; } = [];

        /// <summary>The domain row every read of the data stores domain answers with, the same object each time, when one is set.</summary>
        public global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration? SharedDomainRow { get; init; }

        /// <summary>The containers read as rows, one entry per statement, in order.</summary>
        public List<string> RowReads { get; } = [];

        private readonly Dictionary<string, List<Dictionary<string, object?>>> _rows = new(StringComparer.Ordinal);

        /// <summary>Holds the rows a container answers a read with, selected by the <c>In</c> conditions the read carries.</summary>
        public void SeedRows(string container, params Dictionary<string, object?>[] rows) => _rows[container] = [.. rows];

        public IReadOnlyList<IDataStore> DataStores => _stores;

        public void Seed(Type rowType, params object[] rows) => _childrenByType[rowType] = rows.ToList();

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised in compose-read tests; delegates to existing implementation.
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>)) return Task.FromResult(GenericResult<T>.Success((T)(object)new[] { new DomainConfiguration { Id = _originalId, Name = "AuthDb", Implementation = "MsSql" } }));

            // The data stores domain reads its rows as its own record, which carries the fields its list shows
            // (FDW-1003); a double that answered only the plain domain record would answer this read with no row.
            // With a shared row the same object is handed back on every read, as the gateway's cache does.
            if (typeof(T) == typeof(IEnumerable<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>) && SharedDomainRow is not null)
                return Task.FromResult(GenericResult<T>.Success((T)(object)new[] { SharedDomainRow }));

            if (typeof(T) == typeof(IEnumerable<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new[] { new global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration { Id = _originalId, Name = "AuthDb", Implementation = "MsSql" } }));

            if (typeof(T) == typeof(IEnumerable<DataStoreImplementationConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new List<DataStoreImplementationConfiguration> { _cacheRows ? _header : new DataStoreImplementationConfiguration { Id = _originalId, Name = _header.Name } }));

            // A set read: rows as columns. With cacheRows the same row objects are handed back on every read, as the gateway's cache does;
            // without, a copy of each.
            if (typeof(T) == typeof(IEnumerable<Dictionary<string, object?>>))
            {
                RowReads.Add(target.Container);
                var held = _rows.GetValueOrDefault(target.Container) ?? [];
                var selected = held.Where(row => Selected(command, row)).ToList();
                return Task.FromResult(GenericResult<T>.Success((T)(object)selected
                    .Select(row => _cacheRows ? row : new Dictionary<string, object?>(row, StringComparer.Ordinal)).ToList()));
            }

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        // A row is selected when every In condition names a column it holds the value of, as a store compares them.
        private static bool Selected(IDataCommand command, Dictionary<string, object?> row)
        {
            if (command is not IQueryCommand { Filter.Root: { } root })
                return true;

            return Conditions(root)
                .Where(c => string.Equals(c.Operator.SqlOperator, "IN", StringComparison.Ordinal) && c.Value is System.Collections.IEnumerable and not string)
                .All(c => row.TryGetValue(c.PropertyName, out var value)
                          && ((System.Collections.IEnumerable)c.Value!).Cast<object?>().Any(wanted => string.Equals(Text(wanted), Text(value), StringComparison.Ordinal)));
        }

        private static string Text(object? value) => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

        private static IEnumerable<FilterCondition> Conditions(IFilterNode node)
        {
            if (node is FilterCondition condition)
            {
                yield return condition;
                yield break;
            }

            if (node is FilterGroup group)
            {
                foreach (var nested in group.Nodes.SelectMany(Conditions))
                    yield return nested;
            }
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        // The typed-list child read — record the requested row-type and return the seeded rows, regardless of
        // the JOIN filter (the new cascade joins on owner.Id rather than a single materialized RowId value).
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            ChildReads.Add(rowType);
            if (rowType == typeof(DataPathConfiguration) && !HasOwnerId(((QueryCommand<object>)command).Filter?.Root, _originalId)) return Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));
            var rows = _childrenByType.TryGetValue(rowType, out var list) ? list : [];
            return Task.FromResult(GenericResult<IEnumerable<object>>.Success(rows));
        }

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("This fixture only routes container commands.");

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions are not used by configuration reads.");

        public Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Vectorized commands are not used by configuration reads.");

        // Why: the schema tree mirrors ConfigurationDb's data.* schema. Each OWNER container (DataStore,
        // DataPath) carries the Physical (RowId) + Logical (Id) keys the cascade resolves to build the
        // child JOIN; the leaf DataContainer is given the same keys so a nested recurse into it never
        // mis-resolves. Modeled on the calc-domain provider test's BuildTree.
        private static bool HasOwnerId(object? node, Guid id)
        {
            if (node is FilterCondition condition)
                return condition.PropertyName == "DataStore.Id" && Equals(condition.Value, id);
            if (node is FilterGroup group)
                return group.Nodes.Any(child => HasOwnerId(child, id));
            return false;
        }
        private static IDataStore BuildTree()
        {
            // DataContainer owns its Fields, Keys and PagingSettings, and a Key owns its KeyFields, as data.* declares.
            var fieldContainer = Container("DataContainerField", [],
                [Key("Physical", "PK_DataContainerField", "RowId", null), Key("Logical", "AK_DataContainerField", "Id", null)]);
            var pagingSettingContainer = Container("DataContainerPagingSetting", [],
                [Key("Physical", "PK_DataContainerPagingSetting", "RowId", null), Key("Logical", "AK_DataContainerPagingSetting", "Id", null)]);
            var keyFieldContainer = Container("DataContainerKeyField", [],
                [Key("Physical", "PK_DataContainerKeyField", "RowId", null), Key("Logical", "AK_DataContainerKeyField", "Id", null)]);
            var keyContainer = Container("DataContainerKey",
                [Binding("DataContainerKeyRowId", keyFieldContainer)],
                [Key("Physical", "PK_DataContainerKey", "RowId", null), Key("Logical", "AK_DataContainerKey", "Id", null)]);
            var containerContainer = Container("DataContainer",
                [Binding("DataContainerRowId", fieldContainer), Binding("DataContainerRowId", keyContainer), Binding("DataContainerRowId", pagingSettingContainer)],
                [Key("Physical", "PK_DataContainer", "RowId", null), Key("Logical", "AK_DataContainer", "Id", null)]);

            // DataPath owns Containers (joined on DataPathRowId) — it must carry Physical + Logical keys.
            var policyContainer = Container("DataPathPolicy", [],
                [Key("Physical", "PK_DataPathPolicy", "RowId", null), Key("Logical", "AK_DataPathPolicy", "Id", null)]);
            var segmentContainer = Container("DataPathSegment", [],
                [Key("Physical", "PK_DataPathSegment", "RowId", null), Key("Logical", "AK_DataPathSegment", "Id", null)]);
            var handlerOverrideContainer = Container("FileTypeHandlerOverride", [],
                [Key("Physical", "PK_FileTypeHandlerOverride", "RowId", null), Key("Logical", "AK_FileTypeHandlerOverride", "Id", null)]);
            // DataStoreWorkingPath is a second child of the implementation row, joined on DataStoreConfigurationRowId as data.DataStoreWorkingPath declares (FDW-6394).
            var workingPathContainer = Container("DataStoreWorkingPath", [],
                [Key("Physical", "PK_DataStoreWorkingPath", "RowId", null), Key("Logical", "AK_DataStoreWorkingPath", "Id", null)]);
            var pathContainer = Container("DataPath",
                [Binding("DataPathRowId", containerContainer), Binding("DataPathRowId", policyContainer), Binding("DataPathRowId", segmentContainer), Binding("DataPathRowId", handlerOverrideContainer)],
                [Key("Physical", "PK_DataPath", "RowId", null), Key("Logical", "AK_DataPath", "Id", null)]);

            // DataStore owns its implementation row (joined on DataStoreRowId); the implementation row
            // owns the Paths (joined on DataStoreConfigurationRowId), as data.DataPath declares.
            var storeReferencing = new List<ReferencingKeyBinding>();
            var storeContainer = Container("DataStore",
                storeReferencing,
                [Key("Physical", "PK_DataStore", "RowId", null), Key("Logical", "AK_DataStore", "Id", null)]);

            var implementationContainer = Container("DataStoreConfiguration", [Binding("DataStoreConfigurationRowId", pathContainer), Binding("DataStoreConfigurationRowId", workingPathContainer)], [Key("Physical", "PK_Impl", "RowId", null), Key("Logical", "AK_Impl", "Id", null), Key("Foreign", "FK_Impl", "DataStoreRowId", storeContainer)]);
            storeReferencing.Add(Binding("DataStoreRowId", implementationContainer));
            var containers = new List<IDataContainer> { storeContainer, implementationContainer, pathContainer, containerContainer, fieldContainer, pagingSettingContainer, keyContainer, keyFieldContainer, policyContainer, segmentContainer, handlerOverrideContainer, workingPathContainer };

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("data");
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            {
                var c = containers.FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.Ordinal));
                return c is null ? GenericResult<IDataContainer>.Failure(Fdw.Services.Configuration.Logging.DefaultConfigurationProviderLog.ContainerNotFoundInStore(NullLogger.Instance, "Test", n, "ConfigurationDb")) : GenericResult<IDataContainer>.Success(c);
            });
            foreach (var c in containers)
                Mock.Get(c).Setup(x => x.Parent).Returns(path.Object);

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("ConfigurationDb");
            store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "data", StringComparison.Ordinal)
                    ? GenericResult<IDataNodePath>.Success(path.Object)
                    : GenericResult<IDataNodePath>.Failure(Fdw.Services.Configuration.Logging.DefaultConfigurationProviderLog.ContainerNotFoundInStore(NullLogger.Instance, "Test", n, "ConfigurationDb")));
            return store.Object;
        }

        private static IDataContainer Container(
            string name, IReadOnlyList<ReferencingKeyBinding> referencing, IReadOnlyList<IContainerKey>? keys)
        {
            var c = new Mock<IDataContainer>();
            c.Setup(x => x.Name).Returns(name);
            c.Setup(x => x.Keys).Returns(keys ?? new List<IContainerKey>());
            c.Setup(x => x.Nodes).Returns(new List<IDataNode>());
            c.Setup(x => x.ReferencingKeys).Returns(
                GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
            return c.Object;
        }

        private static ReferencingKeyBinding Binding(string fkColumn, IDataContainer owner)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(fkColumn);
            // Why: an owner is found by the key that references its Physical key (RowId); a key that
            // names no referenced field is not an ownership key and the cascade skips it.
            var referenced = new Mock<IDataField>();
            referenced.Setup(f => f.Name).Returns("RowId");
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            keyField.Setup(k => k.ReferencedField).Returns(referenced.Object);
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyName).Returns($"FK_{fkColumn}_{owner.Name}");
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            return new ReferencingKeyBinding(key.Object, owner);
        }

        private static IContainerKey Key(string keyType, string keyName, string localField, IDataContainer? referenced)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            // Why: KeyType is the abstract KeyTypeBase TypeOption — use the real concrete instances so
            // LookupKeyField reads the genuine Name ("Physical"/"Logical").
            KeyTypeBase kt = keyType switch
            {
                "Physical" => new PhysicalKeyType(),
                "Logical" => new LogicalKeyType(),
                "Foreign" => new ForeignKeyType(),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test")
            };
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(kt);
            key.Setup(k => k.KeyName).Returns(keyName);
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            key.Setup(k => k.ReferencedContainer).Returns(referenced);
            return key.Object;
        }

        // The fixture addresses every call; a gateway's generic command surface carries no address,
        // so nothing here routes one and reaching these would be the test lying about the seam.
        string IPlatformService.Id => "ComposingReadGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");
    }

    // Why the gateway answers for any connection: these tests exercise what a provider does with
    // its gateway, not which one it selects.
    private static AnyConnectionGateways GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;
        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;
        public int KeyChunkSize => 1000;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);
        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}











