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
using Fdw.Services.Data;

namespace Fdw.Services.Tests.Configuration;

[Collection(nameof(ServicesTestCollection))]
public class DefaultConfigurationProviderTests
{
    private static ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand> MakeProvider(
        TestDualConfig[] systemConfigs,
        TestDualConfig[] userConfigs)
    {
        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestDualConfig>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestDualConfig>>.Success(userConfigs));

        var gatewayProvider = GatewayProviderFor(mockGateway.Object);

        return new ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand>>.Instance,
            gatewayProvider,
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
        store.Setup(s => s.Name).Returns("PlatformConfiguration");
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

        var provider = new ImplementationConfigurationProviderBase<TestContainerConfiguration, TestContainerCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestContainerConfiguration, TestContainerCommand>>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "PlatformConfiguration",
            "data");

        var result = await provider.Get("Owner", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Fields.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
    }

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

        var provider = new ImplementationConfigurationProviderBase<TestContainerConfiguration, TestContainerCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestContainerConfiguration, TestContainerCommand>>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "PlatformConfiguration",
            "data");

        var result = await provider.Get("Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Admin");
    }

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
        store.Setup(s => s.Name).Returns("PlatformConfiguration");
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

        var provider = new ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand>>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "TestStore",
            "cfg");

        var result = await provider.Delete(Guid.Empty, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        mockGateway.Verify(g => g.InvalidateCachedResults(It.IsAny<DataStoreTarget>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void InvalidateCacheAsksTheGatewayToDropThisContainer()
    {
        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        new ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestDualConfig, TestConfigurationCommand>>.Instance,
            GatewayProviderFor(mockGateway.Object),
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

        var provider = new ImplementationConfigurationProviderBase<TestKvpConfiguration, TestKvpCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestKvpConfiguration, TestKvpCommand>>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "PlatformConfiguration",
            "conn");

        var result = await provider.Save(owner, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        kvpSaves.Count.ShouldBe(2);
        kvpSaves.ShouldAllBe(s => s.Target == new DataStoreTarget("PlatformConfiguration", "conn", "TestKvpChild"));
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

        var provider = new ImplementationConfigurationProviderBase<TestContainerConfiguration, TestContainerCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestContainerConfiguration, TestContainerCommand>>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "PlatformConfiguration",
            "data");

        var result = await provider.Save(owner, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        childSaves.Count.ShouldBe(2);
        childSaves.ShouldAllBe(s => s.Target == new DataStoreTarget("PlatformConfiguration", "data", "TestContainerField"));
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

    [TypeOption(typeof(ConfigurationCommands), "TestDualConfig")]
    public sealed class TestConfigurationCommand : ConfigurationCommandBase<TestDualConfig>
    {
        public TestConfigurationCommand() : base("TestDualConfig") { }
    }

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

    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
