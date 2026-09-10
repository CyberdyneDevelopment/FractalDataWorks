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
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Pins what an implementation provider does with the rows it reads and writes: it lists them,
/// composes their children onto them, and cascades those children back out on a save.
/// </summary>
/// <remarks>
/// A read keyed by id is the domain's id — the row is reached through the domain it hangs from, so
/// nothing here looks a configuration up by name. That is the domain provider's job, and the name
/// lives on the domain row.
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public class DefaultConfigurationProviderTests
{
    private static TestDualConfigProvider MakeProvider(params TestDualConfig[] rows)
    {
        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns((IReadOnlyList<IDataStore>)Array.Empty<IDataStore>());
        mockGateway
            .Setup(g => g.Execute<IEnumerable<TestDualConfig>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TestDualConfig>>.Success(rows));

        return new TestDualConfigProvider(GatewayProviderFor(mockGateway.Object));
    }

    // ========================================================================
    // Get(id)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetByIdReturnsNullForEmptyGuid()
    {
        var result = await MakeProvider().Get(Guid.Empty, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ========================================================================
    // Get() — every row this implementation holds
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetReturnsEveryRowTheStoreHolds()
    {
        var row = new TestDualConfig { Id = Guid.NewGuid(), Name = "UserDb" };

        var result = await MakeProvider(row).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ShouldHaveSingleItem().ShouldBe(row);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetReturnsEmptyWhenTheStoreHoldsNothing()
    {
        var result = await MakeProvider().Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // ========================================================================
    // Get() — composes typed-list children onto each row it read
    // ========================================================================

    private static List<IDataStore> BuildOwnerKeyTree(string containerName)
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
    public async Task GetComposesTypedListChildrenOntoTheRowItRead()
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

        var result = await new TestContainerProvider(GatewayProviderFor(mockGateway.Object))
            .Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ShouldHaveSingleItem().Fields
            .Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetResolvesOwnerKeysWhenTheContainerAlsoCarriesASelfReferencingFk()
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
            .ReturnsAsync(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));

        var result = await new TestContainerProvider(GatewayProviderFor(mockGateway.Object))
            .Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ShouldHaveSingleItem().Name.ShouldBe("Admin");
    }

    private static List<IDataStore> BuildSelfReferencingKeyTree(string containerName)
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
        mockGateway.Setup(g => g.DataStores).Returns((IReadOnlyList<IDataStore>)Array.Empty<IDataStore>());
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

        var result = await new TestKvpProvider(GatewayProviderFor(mockGateway.Object))
            .Save(owner, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        kvpSaves.Count.ShouldBe(2);
        kvpSaves.ShouldAllBe(s => s.Target == new DataStoreTarget("PlatformConfiguration", "conn", "TestKvpChild"));
        kvpSaves.Select(s => s.Command.Data.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
        foreach (var (command, _) in kvpSaves)
        {
            command.Data.Value.ShouldBe(command.Data.Name == "Alpha" ? "1" : "2");
            // Strip(TestKvpConfiguration)+"Id" = "TestKvpId" — the same FK-name convention
            // CascadeCollections applies to typed-list children.
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
        mockGateway.Setup(g => g.DataStores).Returns((IReadOnlyList<IDataStore>)Array.Empty<IDataStore>());
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

        var result = await new TestContainerProvider(GatewayProviderFor(mockGateway.Object))
            .Save(owner, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        childSaves.Count.ShouldBe(2);
        childSaves.ShouldAllBe(s => s.Target == new DataStoreTarget("PlatformConfiguration", "data", "TestContainerField"));
        childSaves.ShouldAllBe(s => s.Command.Data.Id != Guid.Empty);
        // Strip(TestContainerConfiguration)+"Id" = "TestContainerId" — the same FK-name convention
        // CascadeCollections applies to KVP children (FDW-547) and DataSet.Fields.
        childSaves.ShouldAllBe(s => s.Command.Data.TestContainerId == owner.Id);
        childSaves.Select(s => s.Command.Data.Name).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(["Alpha", "Beta"]);
    }

    // ========================================================================
    // Test types
    // ========================================================================

    public sealed class TestDualConfig : ITestDualConfigImplementationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Implementation { get; set; } = "Default";
        public string? Description { get; init; }
    }

    private sealed class TestDualConfigProvider
        : ImplementationProviderBase<TestDualConfig, ITestDualConfigImplementationConfiguration>
    {
        public TestDualConfigProvider(IConfigurationGatewayProvider gatewayProvider)
            : base(
                NullLogger<ImplementationProviderBase<TestDualConfig, ITestDualConfigImplementationConfiguration>>.Instance,
                gatewayProvider,
                "TestStore",
                "cfg",
                "TestDualConfig")
        {
        }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestDualConfig")]
    public sealed class TestConfigurationCommand : ConfigurationCommandBase<TestDualConfig>
    {
        public TestConfigurationCommand() : base("TestDualConfig") { }
    }

    [GenerateMapper]
    public sealed class TestKvpConfiguration : ITestKvpImplementationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Implementation { get; set; } = "Default";

        [ConfigurationChildTable("TestKvpChild")]
        public IDictionary<string, string?> Properties { get; set; } = new Dictionary<string, string?>(StringComparer.Ordinal);
    }

    private sealed class TestKvpProvider
        : ImplementationProviderBase<TestKvpConfiguration, ITestKvpImplementationConfiguration>
    {
        public TestKvpProvider(IConfigurationGatewayProvider gatewayProvider)
            : base(
                NullLogger<ImplementationProviderBase<TestKvpConfiguration, ITestKvpImplementationConfiguration>>.Instance,
                gatewayProvider,
                "PlatformConfiguration",
                "conn",
                "TestKvp")
        {
        }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestKvp")]
    public sealed class TestKvpCommand : ConfigurationCommandBase<TestKvpConfiguration>
    {
        public TestKvpCommand() : base("TestKvp") { }
    }

    [GenerateMapper]
    public sealed class TestContainerConfiguration : ITestContainerImplementationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Implementation { get; set; } = "Default";

        public List<TestContainerFieldConfiguration> Fields { get; set; } = [];
    }

    private sealed class TestContainerProvider
        : ImplementationProviderBase<TestContainerConfiguration, ITestContainerImplementationConfiguration>
    {
        public TestContainerProvider(IConfigurationGatewayProvider gatewayProvider)
            : base(
                NullLogger<ImplementationProviderBase<TestContainerConfiguration, ITestContainerImplementationConfiguration>>.Instance,
                gatewayProvider,
                "PlatformConfiguration",
                "data",
                "TestContainer")
        {
        }
    }

    [GenerateMapper]
    public sealed class TestContainerFieldConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

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

    private static AnyConnectionGateways GatewayProviderFor(IConfigurationGateway gateway)
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
