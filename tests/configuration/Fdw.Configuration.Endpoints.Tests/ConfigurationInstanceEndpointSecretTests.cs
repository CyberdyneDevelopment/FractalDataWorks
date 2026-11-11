using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.Endpoints.Tests;

/// <summary>
/// The configuration-instance endpoints never read a container whose rows hang from another container's rows (a connection's key/value
/// property table keeps its secrets in Value), and the get endpoint masks a value only when the owning configuration type declares its
/// member <see cref="SecretValueAttribute"/>; a column whose name only looks secret is not masked by its name (FDW-4644, FDW-3002).
/// </summary>
[Trait("Category", "Security")]
public sealed class ConfigurationInstanceEndpointSecretTests
{
    private static readonly Guid InstanceId = new("aaaaaaaa-0000-0000-0000-000000000001");

    /// <summary>An owning configuration type: Password is declared secret; ApiKey only looks secret.</summary>
    private sealed class SqlConfiguration
    {
        [SecretValue]
        public string? Password { get; set; }

        public string? ApiKey { get; set; }

        public string? Server { get; set; }
    }

    private sealed class GetEndpoint(IConfigurationGatewayProvider gateways, IConfigurationContainerLookup lookup)
        : GetConfigurationInstanceEndpointBase(gateways, lookup, NullLogger<GetConfigurationInstanceEndpointBase>.Instance);

    private sealed class DeclaredGetEndpoint(IConfigurationGatewayProvider gateways, IConfigurationContainerLookup lookup)
        : GetConfigurationInstanceEndpointBase(gateways, lookup, NullLogger<GetConfigurationInstanceEndpointBase>.Instance)
    {
        protected override IGenericResult<Type> ConfigurationTypeOf(IDataContainer container)
            => GenericResult<Type>.Success(typeof(SqlConfiguration));
    }

    private sealed class ListEndpoint(IConfigurationGatewayProvider gateways, IConfigurationContainerLookup lookup)
        : ListConfigurationInstancesEndpointBase(gateways, lookup, NullLogger<ListConfigurationInstancesEndpointBase>.Instance);

    private static IDataField Field(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    private static IContainerKey Key(KeyTypeBase type, string localField, IDataContainer? referenced = null, string? referencedField = null)
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(Field(localField));
        keyField.Setup(k => k.ReferencedField).Returns(referencedField is null ? null : Field(referencedField));
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyType).Returns(type);
        key.Setup(k => k.KeyFields).Returns([keyField.Object]);
        key.Setup(k => k.ReferencedContainer).Returns(referenced);
        return key.Object;
    }

    private static IDataContainer Container(string name, params IContainerKey[] keys)
    {
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns("conn");
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Parent).Returns(path.Object);
        container.Setup(c => c.Keys).Returns(keys);
        return container.Object;
    }

    // conn.Connection: a top-level configuration (RowId Physical, Id Logical).
    private static IDataContainer Connection() => Container("Connection", Key(new PhysicalKeyType(), "RowId"), Key(new LogicalKeyType(), "Id"));

    // conn.MsSqlConnectionProperty: Name/Value rows under one version of their connection body, by its Physical key.
    private static IDataContainer Property(IDataContainer owner)
        => Container("MsSqlConnectionProperty",
            Key(new PhysicalKeyType(), "RowId"),
            Key(new ForeignKeyType(), "MsSqlConnectionConfigurationRowId", owner, "RowId"));

    private static Mock<IConfigurationContainerLookup> Lookup(params IDataContainer[] containers)
    {
        var lookup = new Mock<IConfigurationContainerLookup>();
        lookup.Setup(l => l.ByCategory("conn")).Returns(containers);
        lookup.Setup(l => l.All()).Returns(containers);
        return lookup;
    }

    private static (Mock<IConfigurationGatewayProvider> Provider, Mock<IConfigurationGateway> Gateway) Gateways(params Dictionary<string, object?>[] rows)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        gateway.Setup(g => g.Execute<IEnumerable<ConfigurationRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ConfigurationRecord>>.Success([]));
        var provider = new Mock<IConfigurationGatewayProvider>();
        provider.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return (provider, gateway);
    }

    private static Dictionary<string, object?> Row(string name) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = InstanceId,
        ["Name"] = name,
        ["CreatedAt"] = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
        ["Server"] = "db1",
        ["Password"] = "p@ss",
        ["ApiKey"] = "key-123",
    };

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACategoryOfOnlyAKeyValueChildContainerIsRefusedAndNeverRead()
    {
        var (provider, gateway) = Gateways(Row("Password"));
        var endpoint = Factory.Create<GetEndpoint>(provider.Object, Lookup(Property(Connection())).Object);

        await endpoint.HandleAsync(new GetConfigurationInstanceRequest { Category = "conn", Name = "Password" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OnlyTheTopLevelContainerOfACategoryIsReadNeverItsKeyValueChild()
    {
        var (provider, gateway) = Gateways();
        var connection = Connection();
        var endpoint = Factory.Create<DeclaredGetEndpoint>(provider.Object, Lookup(connection, Property(connection)).Object);

        await endpoint.HandleAsync(new GetConfigurationInstanceRequest { Category = "conn", Name = "Password" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADeclaredSecretMemberIsMasked()
    {
        var (provider, _) = Gateways(Row("Warehouse"));
        var endpoint = Factory.Create<DeclaredGetEndpoint>(provider.Object, Lookup(Connection()).Object);

        await endpoint.HandleAsync(new GetConfigurationInstanceRequest { Category = "conn", Name = "Warehouse" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Values["Password"].ShouldBe("********");
        endpoint.Response.Values["Server"].ShouldBe("db1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AColumnWhoseNameOnlyLooksSecretIsNotMaskedByItsName()
    {
        var (provider, _) = Gateways(Row("Warehouse"));
        var endpoint = Factory.Create<DeclaredGetEndpoint>(provider.Object, Lookup(Connection()).Object);

        await endpoint.HandleAsync(new GetConfigurationInstanceRequest { Category = "conn", Name = "Warehouse" }, TestContext.Current.CancellationToken);

        endpoint.Response.Values["ApiKey"].ShouldBe("key-123");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AContainerWithNoDeclaredConfigurationTypeReturnsNoneOfItsValues()
    {
        var (provider, gateway) = Gateways(Row("Warehouse"));
        var endpoint = Factory.Create<GetEndpoint>(provider.Object, Lookup(Connection()).Object);

        await endpoint.HandleAsync(new GetConfigurationInstanceRequest { Category = "conn", Name = "Warehouse" }, TestContext.Current.CancellationToken);

        // The row was found and read; the refusal (InstanceTypeNotDeclared, a configuration error) comes after, not a failed query.
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheListNeverReadsAKeyValueChildContainer()
    {
        var (provider, gateway) = Gateways();
        var connection = Connection();
        var endpoint = Factory.Create<ListEndpoint>(provider.Object, Lookup(connection, Property(connection)).Object);

        await endpoint.HandleAsync(new ListConfigurationInstancesRequest { Category = "conn" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        gateway.Verify(g => g.Execute<IEnumerable<ConfigurationRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
