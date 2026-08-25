using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

[Collection(nameof(ServicesTestCollection))]
public class DefaultConfigurationProviderTests
{
    private static DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand> MakeProvider(
        TestDualConfig[] systemConfigs,
        TestDualConfig[] userConfigs)
    {
        // Why: The two-arity constructor requires Lazy<IConfigurationGateway>, dataStoreName,
        // and pathName. The TCommand generic arg replaces IConfigurationType — the command encodes
        // the table name. We wire up a mock gateway that returns userConfigs for any IDataCommand.
        var mockGateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        mockGateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestDualConfig>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestDualConfig>>.Success(userConfigs));

        var lazyGateway = new Lazy<IConfigurationGateway>(() => mockGateway.Object);

        return new DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>(
            NullLogger<DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>>.Instance,
            lazyGateway,
            "TestStore",
            "cfg");
    }

    // ========================================================================
    // Constructor
    // ========================================================================

    // ========================================================================
    // Get(name)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameReturnsCfgConfigWhenOnlyInCfg()
    {
        var userConfig = new TestDualConfig { Id = Guid.NewGuid(), Name = "UserDb" };

        var provider = MakeProvider([], [userConfig]);

        var result = await provider.Get("UserDb", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBe(userConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameReturnsNullWhenNotInSystemAndNoUserCache()
    {
        // Why: The mock DataGateway returns all user configs for any query (can't filter),
        // so we test system-only miss with empty user cache to verify null is returned.
        var provider = MakeProvider(
            [new TestDualConfig { Id = Guid.NewGuid(), Name = "Other" }],
            []);

        var result = await provider.Get("NonExistent", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameReturnsNullForNullName()
    {
        var provider = MakeProvider([], []);

        var result = await provider.Get((string)null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameReturnsNullForWhitespaceName()
    {
        var provider = MakeProvider([], []);

        var result = await provider.Get("   ", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ========================================================================
    // Get(id)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetByIdReturnsNullForEmptyGuid()
    {
        var provider = MakeProvider([], []);

        var result = await provider.Get(Guid.Empty, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ========================================================================
    // Get(name) / Get(id) — still compose typed-list children via the extracted
    // ComposeAggregate hook (FDW-558 behavior-preservation regression)
    // ========================================================================

    // Why: builds the minimal owner-key metadata tree (Physical=RowId, Logical=Id) so
    // ResolveOwnerKeyColumns("TestContainer") resolves and LoadChildrenInto actually issues the child
    // JOIN instead of silently skipping (NoSuitableKeyForContainer) — without this, a Get(name) call
    // that happens to leave Fields empty would prove nothing about whether ComposeAggregate ran.
    private static IReadOnlyList<IDataStore> BuildOwnerKeyTree(string containerName)
    {
        var physicalField = new Mock<IDataField>();
        physicalField.Setup(f => f.Name).Returns("RowId");
        var physicalKeyField = new Mock<IContainerKeyField>();
        physicalKeyField.Setup(k => k.LocalField).Returns(physicalField.Object);
        var physicalKey = new Mock<IContainerKey>();
        physicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Physical);
        physicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { physicalKeyField.Object });
        physicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var logicalField = new Mock<IDataField>();
        logicalField.Setup(f => f.Name).Returns("Id");
        var logicalKeyField = new Mock<IContainerKeyField>();
        logicalKeyField.Setup(k => k.LocalField).Returns(logicalField.Object);
        var logicalKey = new Mock<IContainerKey>();
        logicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Logical);
        logicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { logicalKeyField.Object });
        logicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(containerName);
        container.Setup(c => c.Keys).Returns(new List<IContainerKey> { physicalKey.Object, logicalKey.Object });

        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns("data");
        path.Setup(p => p.Containers).Returns(new List<IDataContainer> { container.Object });
        path.Setup(p => p.Container(It.Is<string>(n => string.Equals(n, containerName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(container.Object));
        path.Setup(p => p.Container(It.Is<string>(n => !string.Equals(n, containerName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("ConfigurationDb");
        store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
        store.Setup(s => s.Path(It.Is<string>(n => string.Equals(n, "data", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Success(path.Object));

        return new List<IDataStore> { store.Object };
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameStillComposesTypedListChildrenAfterComposeAggregateExtraction()
    {
        var owner = new TestContainerConfiguration { Id = Guid.NewGuid(), Name = "Owner" };

        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(BuildOwnerKeyTree("TestContainer"));
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestContainerConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestContainerConfiguration>>.Success([owner]));
        mockGateway
            .Setup(g => g.Execute(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => string.Equals(t.Container, "TestContainerField", StringComparison.Ordinal)),
                typeof(TestContainerFieldConfiguration),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<object>>.Success(new object[]
            {
                new TestContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Alpha", TypeId = "String" },
                new TestContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Beta", TypeId = "Int32" },
            }));

        var provider = new DefaultConfigurationProvider<TestContainerConfiguration, TestContainerCommand>(
            NullLogger<DefaultConfigurationProvider<TestContainerConfiguration, TestContainerCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            "ConfigurationDb",
            "data");

        var result = await provider.Get("Owner", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // Why: this is the exact behavior A1 (extracting ComposeAggregate) must preserve — Get(string)
        // used to call ComposeTypedBody+ComposeChildren inline; it now calls them via the shared hook.
        // A regression here would mean the extraction silently dropped the child cascade.
        result.Value!.Fields.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
    }

    // Why (FDW-601): a root header that carries a self-referencing hierarchy FK (e.g.
    // authz.Role.ParentRoleRowId → Role) must still resolve by name. Before the fix, FindForeignKey
    // returned the self-FK as a "parent", ResolveParentJoin reported HasParent=true, and
    // GetHeaderByName refused name resolution (TypedBodyNotResolvableByName) → Get(name) 404'd.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameResolvesRootContainerWithSelfReferencingHierarchyFk()
    {
        var owner = new TestContainerConfiguration { Id = Guid.NewGuid(), Name = "Admin" };

        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(BuildSelfReferencingKeyTree("TestContainer"));
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestContainerConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestContainerConfiguration>>.Success([owner]));
        mockGateway
            .Setup(g => g.Execute(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => string.Equals(t.Container, "TestContainerField", StringComparison.Ordinal)),
                typeof(TestContainerFieldConfiguration),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<object>>.Success(System.Array.Empty<object>()));

        var provider = new DefaultConfigurationProvider<TestContainerConfiguration, TestContainerCommand>(
            NullLogger<DefaultConfigurationProvider<TestContainerConfiguration, TestContainerCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            "ConfigurationDb",
            "data");

        var result = await provider.Get("Admin", TestContext.Current.CancellationToken);

        // Why: the self-FK must be skipped so this is a by-name root read, not the
        // TypedBodyNotResolvableByName failure that produced the /roles/{name} 404.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Admin");
    }

    // Why (FDW-601): mirrors BuildOwnerKeyTree but adds a self-referencing hierarchy FK
    // (LocalField ParentRowId → this same container) — the exact shape that misclassified authz.Role.
    private static IReadOnlyList<IDataStore> BuildSelfReferencingKeyTree(string containerName)
    {
        var physicalField = new Mock<IDataField>();
        physicalField.Setup(f => f.Name).Returns("RowId");
        var physicalKeyField = new Mock<IContainerKeyField>();
        physicalKeyField.Setup(k => k.LocalField).Returns(physicalField.Object);
        var physicalKey = new Mock<IContainerKey>();
        physicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Physical);
        physicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { physicalKeyField.Object });
        physicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var logicalField = new Mock<IDataField>();
        logicalField.Setup(f => f.Name).Returns("Id");
        var logicalKeyField = new Mock<IContainerKeyField>();
        logicalKeyField.Setup(k => k.LocalField).Returns(logicalField.Object);
        var logicalKey = new Mock<IContainerKey>();
        logicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Logical);
        logicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { logicalKeyField.Object });
        logicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var selfRefField = new Mock<IDataField>();
        selfRefField.Setup(f => f.Name).Returns("ParentRowId");
        var selfRefKeyField = new Mock<IContainerKeyField>();
        selfRefKeyField.Setup(k => k.LocalField).Returns(selfRefField.Object);
        var selfContainerRef = new Mock<IDataContainer>();
        selfContainerRef.Setup(c => c.Name).Returns(containerName);
        var foreignKey = new Mock<IContainerKey>();
        foreignKey.Setup(k => k.KeyType).Returns(KeyTypes.Foreign);
        foreignKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { selfRefKeyField.Object });
        foreignKey.Setup(k => k.ReferencedContainer).Returns(selfContainerRef.Object);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(containerName);
        container.Setup(c => c.Keys).Returns(new List<IContainerKey> { physicalKey.Object, logicalKey.Object, foreignKey.Object });

        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns("data");
        path.Setup(p => p.Containers).Returns(new List<IDataContainer> { container.Object });
        path.Setup(p => p.Container(It.Is<string>(n => string.Equals(n, containerName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(container.Object));
        path.Setup(p => p.Container(It.Is<string>(n => !string.Equals(n, containerName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("ConfigurationDb");
        store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
        store.Setup(s => s.Path(It.Is<string>(n => string.Equals(n, "data", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Success(path.Object));

        return new List<IDataStore> { store.Object };
    }

    // ========================================================================
    // GetAll() — deduplication by name, system wins
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetAllReturnsOnlyUserWhenNoSystemConfigs()
    {
        var userConfig = new TestDualConfig { Id = Guid.NewGuid(), Name = "UserDb" };

        var provider = MakeProvider([], [userConfig]);

        var getAllResult = await provider.Get(TestContext.Current.CancellationToken);
        getAllResult.IsSuccess.ShouldBeTrue();
        var result = getAllResult.Value!.ToList();

        result.Count.ShouldBe(1);
        result[0].ShouldBe(userConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetAllReturnsEmptyWhenBothSourcesEmpty()
    {
        var provider = MakeProvider([], []);

        var getAllResult = await provider.Get(TestContext.Current.CancellationToken);

        getAllResult.IsSuccess.ShouldBeTrue();
        getAllResult.Value.ShouldNotBeNull();
        getAllResult.Value.ShouldBeEmpty();
    }

    // ========================================================================
    // Cache invalidation
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task DeleteFailsForEmptyGuid()
    {
        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        var provider = new DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>(
            NullLogger<DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            "TestStore",
            "cfg");

        var result = await provider.Delete(Guid.Empty, TestContext.Current.CancellationToken);

        // Why: deleting nothing is a caller error, not a no-op — it used to report Success.
        result.IsSuccess.ShouldBeFalse();
        mockGateway.Verify(g => g.InvalidateCachedResults(It.IsAny<DataStoreTarget>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void InvalidateCacheAsksTheGatewayToDropThisContainer()
    {
        // Why this is the only invalidation a provider still initiates: a write executed through the
        // gateway invalidates itself. A write made inside a transaction cannot — its rows are not
        // visible until commit — so the committer calls this afterwards.
        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        new DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>(
            NullLogger<DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            "TestStore",
            "cfg").InvalidateCache();

        mockGateway.Verify(
            g => g.InvalidateCachedResults(
                It.Is<DataStoreTarget>(t => t.Path == "cfg" && t.Container == "TestDualConfig")),
            Times.Once);
    }

    // ========================================================================
    // GetCount
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetCountReflectsDeduplicatedTotal()
    {
        var systemConfig = new TestDualConfig { Id = Guid.NewGuid(), Name = "ControlDb" };
        var userConfig1 = new TestDualConfig { Id = Guid.NewGuid(), Name = "ControlDb" };
        var userConfig2 = new TestDualConfig { Id = Guid.NewGuid(), Name = "UserDb" };

        // 1 system + 2 user, but "ControlDb" collides -> deduped total = 2
        var provider = MakeProvider([systemConfig], [userConfig1, userConfig2]);

        var getAllResult = await provider.Get(TestContext.Current.CancellationToken);
        getAllResult.IsSuccess.ShouldBeTrue();
        getAllResult.Value!.Count.ShouldBe(2);
    }

    // ========================================================================
    // Save — KVP property-collection cascade (FDW-547)
    // ========================================================================

    // Why: the write cascade previously no-op'd on IsPropertyCollection descriptors (CascadeCollections),
    // silently dropping every KVP child row on save. This pins the fix: one ConfigurationSaveCommand
    // <KeyValueRow> per bag entry, each carrying the owner's logical FK via AdditionalColumnValues. A
    // 2-entry bag is MANDATORY here — it locks the sibling-deactivation regression the translator fix
    // also addresses (a single-entry bag would pass even with the old owner-only UPDATE predicate).
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task SaveCascadesKvpChildOneRowPerEntryWithOwnerForeignKey()
    {
        var owner = new TestKvpConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Owner",
            Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Alpha"] = "1",
                ["Beta"] = "2",
            },
        };

        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestKvpConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestKvpConfiguration>>.Success([]));
        mockGateway
            .Setup(g => g.Execute<TestKvpConfiguration>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestKvpConfiguration>.Success(owner));

        var kvpSaves = new List<(ConfigurationSaveCommand<KeyValueRow> Command, DataStoreTarget Target)>();
        mockGateway
            .Setup(g => g.Execute(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand cmd, DataStoreTarget target, CancellationToken _) =>
            {
                if (cmd is ConfigurationSaveCommand<KeyValueRow> kvpCmd)
                    kvpSaves.Add((kvpCmd, target));
                return Task.FromResult<IGenericResult>(GenericResult.Success());
            });

        var provider = new DefaultConfigurationProvider<TestKvpConfiguration, TestKvpCommand>(
            NullLogger<DefaultConfigurationProvider<TestKvpConfiguration, TestKvpCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            "ConfigurationDb",
            "conn");

        var result = await provider.Save(owner, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        kvpSaves.Count.ShouldBe(2);
        kvpSaves.ShouldAllBe(s => s.Target == new DataStoreTarget("ConfigurationDb", "conn", "TestKvpChild"));
        kvpSaves.Select(s => s.Command.Data.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
        foreach (var (command, _) in kvpSaves)
        {
            var expectedValue = command.Data.Name == "Alpha" ? "1" : "2";
            command.Data.Value.ShouldBe(expectedValue);
            // Strip(TestKvpConfiguration)+"Id" = "TestKvpId" — the same FK-name convention CascadeCollections
            // already applies to typed-list children.
            command.AdditionalColumnValues["TestKvpId"].ShouldBe(owner.Id);
        }
    }

    // ========================================================================
    // Save — typed-list child cascade, generic fields (FDW-548)
    // ========================================================================

    // Why: pins the write cascade FDW-548 relies on — a mapper-visible List<T> child collection with no
    // [NotMapped] (mirrors DataContainerConfiguration.Fields / DataSetConfiguration.Fields) gets a
    // typed-list CascadeChildren descriptor, and CascadeCollections stamps the owner's logical FK + mints
    // each child's Id before issuing one ConfigurationSaveCommand<T> per row — the same mechanism proven
    // for FDW-547's KVP children, applied here to a typed-list collection. Two fields are used so a
    // single-row collection couldn't mask an off-by-one in the cascade loop.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task SaveCascadesTypedListChildOneRowPerItemWithOwnerForeignKeyAndMintedId()
    {
        var owner = new TestContainerConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Owner",
            Fields =
            [
                new TestContainerFieldConfiguration { Name = "Alpha", TypeId = "String" },
                new TestContainerFieldConfiguration { Name = "Beta", TypeId = "Int32" },
            ],
        };

        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestContainerConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestContainerConfiguration>>.Success([]));
        mockGateway
            .Setup(g => g.Execute<TestContainerConfiguration>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestContainerConfiguration>.Success(owner));

        var childSaves = new List<(ConfigurationSaveCommand<TestContainerFieldConfiguration> Command, DataStoreTarget Target)>();
        mockGateway
            .Setup(g => g.Execute(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand cmd, DataStoreTarget target, CancellationToken _) =>
            {
                if (cmd is ConfigurationSaveCommand<TestContainerFieldConfiguration> childCmd)
                    childSaves.Add((childCmd, target));
                return Task.FromResult<IGenericResult>(GenericResult.Success());
            });

        var provider = new DefaultConfigurationProvider<TestContainerConfiguration, TestContainerCommand>(
            NullLogger<DefaultConfigurationProvider<TestContainerConfiguration, TestContainerCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            "ConfigurationDb",
            "data");

        var result = await provider.Save(owner, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        childSaves.Count.ShouldBe(2);
        childSaves.ShouldAllBe(s => s.Target == new DataStoreTarget("ConfigurationDb", "data", "TestContainerField"));
        childSaves.ShouldAllBe(s => s.Command.Data.Id != Guid.Empty);
        // Strip(TestContainerConfiguration)+"Id" = "TestContainerId" — the same FK-name convention
        // CascadeCollections already applies to KVP children (FDW-547) and DataSet.Fields.
        childSaves.ShouldAllBe(s => s.Command.Data.TestContainerId == owner.Id);
        childSaves.Select(s => s.Command.Data.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
    }

    // ========================================================================
    // Test types
    // ========================================================================

    public sealed class TestDualConfig : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestSection";
        public string ServiceType => "Test";
        public string? ServiceOptionType => "Default";
        public string? Description { get; init; }
    }

    // Why: ConfigurationCommands is a TypeCollection — TestConfigurationCommand must be registered
    // via [TypeOption] so that DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>
    // can resolve Commands() from ConfigurationCommands.All().OfType<TestConfigurationCommand>().Single().
    [TypeOption(typeof(ConfigurationCommands), "TestDualConfig")]
    public sealed class TestConfigurationCommand : ConfigurationCommandBase<TestDualConfig>
    {
        public TestConfigurationCommand() : base("TestDualConfig") { }
    }

    // Why: a real [GenerateMapper] POCO with a [ConfigurationChildTable] KVP property so the generated
    // ReadDictionary (FDW-547) actually runs — not a hand-rolled stand-in.
    [GenerateMapper]
    public sealed class TestKvpConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestKvp";
        public string ServiceType => "TestKvp";
        public string? ServiceOptionType => "Default";

        [ConfigurationChildTable("TestKvpChild")]
        public IDictionary<string, string?> Properties { get; set; } = new Dictionary<string, string?>(StringComparer.Ordinal);
    }

    [TypeOption(typeof(ConfigurationCommands), "TestKvp")]
    public sealed class TestKvpCommand : ConfigurationCommandBase<TestKvpConfiguration>
    {
        public TestKvpCommand() : base("TestKvp") { }
    }

    // Why: a real [GenerateMapper] parent/child pair mirroring DataContainerConfiguration.Fields —
    // a mapper-visible (no [NotMapped]) List<T> of a type implementing IGenericConfiguration, so the
    // generator emits a typed-list CascadeChildren descriptor (FDW-548), not a KVP one.
    [GenerateMapper]
    public sealed class TestContainerConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestContainer";
        public string ServiceType => "TestContainer";
        public string? ServiceOptionType => "Default";

        public List<TestContainerFieldConfiguration> Fields { get; set; } = [];
    }

    [GenerateMapper]
    public sealed class TestContainerFieldConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestContainerField";
        public string ServiceType => "TestContainer";
        public string? ServiceOptionType => null;

        // Owner FK — set by CascadeCollections via generated SetValue("TestContainerId", owner.Id).
        public Guid TestContainerId { get; set; }
        public string? TypeId { get; set; }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestContainer")]
    public sealed class TestContainerCommand : ConfigurationCommandBase<TestContainerConfiguration>
    {
        public TestContainerCommand() : base("TestContainer") { }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestContainerField")]
    public sealed class TestContainerFieldCommand : ConfigurationCommandBase<TestContainerFieldConfiguration>
    {
        public TestContainerFieldCommand() : base("TestContainerField") { }
    }
}
