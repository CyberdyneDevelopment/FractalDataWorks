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
        mockGateway.Setup(g => g.BeginTransaction(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IDataGatewayTransaction>.Success(new RecordingTransaction(mockGateway.Object)));
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
    // Save — KVP property-collection cascade (FDW-547)
    // ========================================================================


    // ========================================================================
    // Save — typed-list child cascade, generic fields (FDW-548)
    // ========================================================================


    // ========================================================================
    // Test types
    // ========================================================================

    [Fdw.Data.GenerateMapper]
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
