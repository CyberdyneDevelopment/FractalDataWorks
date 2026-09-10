using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Builds a REAL domain configuration provider over a faked configuration store.
/// </summary>
/// <remarks>
/// Why not a mock of the provider: a resolver takes the provider itself, which is sealed and whose
/// reads are not virtual, so nothing can stand in for it. The catalogue is therefore supplied the way
/// production supplies it -- domain rows from the gateway, each dispatched to the implementation
/// provider registered for the row's implementation -- and the test states only what the store holds.
/// </remarks>
internal static class ConfigurationCatalog
{
    private const string OneImplementation = "Default";

    /// <summary>The role catalogue this store holds.</summary>
    public static RoleConfigurationProvider Roles(IEnumerable<IRoleImplementationConfiguration> items)
        => Of<RoleConfigurationProvider, IRoleImplementationConfiguration>(
            gateways => new RoleConfigurationProvider(
                NullLogger<RoleConfigurationProvider>.Instance, gateways, "TestStore"), items);

    /// <summary>A role catalogue whose store cannot be read.</summary>
    public static RoleConfigurationProvider UnreadableRoles(string reason)
        => Unreadable<RoleConfigurationProvider, IRoleImplementationConfiguration>(
            gateways => new RoleConfigurationProvider(
                NullLogger<RoleConfigurationProvider>.Instance, gateways, "TestStore"), reason);

    /// <summary>The permission catalogue this store holds.</summary>
    public static PermissionConfigurationProvider Permissions(IEnumerable<IPermissionImplementationConfiguration> items)
        => Of<PermissionConfigurationProvider, IPermissionImplementationConfiguration>(
            gateways => new PermissionConfigurationProvider(
                NullLogger<PermissionConfigurationProvider>.Instance, gateways, "TestStore"), items);

    /// <summary>The role-to-permission grants this store holds.</summary>
    public static RolePermissionConfigurationProvider RolePermissions(IEnumerable<IRolePermissionImplementationConfiguration> items)
        => Of<RolePermissionConfigurationProvider, IRolePermissionImplementationConfiguration>(
            gateways => new RolePermissionConfigurationProvider(
                NullLogger<RolePermissionConfigurationProvider>.Instance, gateways, "TestStore"), items);

    /// <summary>The user-role assignments this store holds.</summary>
    public static UserRoleConfigurationProvider UserRoles(IEnumerable<IUserRoleImplementationConfiguration> items)
        => Of<UserRoleConfigurationProvider, IUserRoleImplementationConfiguration>(
            gateways => new UserRoleConfigurationProvider(
                NullLogger<UserRoleConfigurationProvider>.Instance, gateways, "TestStore"), items);

    /// <summary>A user-role catalogue whose store cannot be read.</summary>
    public static UserRoleConfigurationProvider UnreadableUserRoles(string reason)
        => Unreadable<UserRoleConfigurationProvider, IUserRoleImplementationConfiguration>(
            gateways => new UserRoleConfigurationProvider(
                NullLogger<UserRoleConfigurationProvider>.Instance, gateways, "TestStore"), reason);

    private static TProvider Of<TProvider, TContract>(
        Func<IConfigurationGatewayProvider, TProvider> create,
        IEnumerable<TContract> items)
        where TProvider : DomainConfigurationProviderBase<TContract>
        where TContract : IImplementationConfiguration
    {
        var rows = new List<DomainConfiguration>();
        var byRow = new Dictionary<Guid, TContract>();
        foreach (var item in items)
        {
            var rowId = Guid.NewGuid();
            byRow[rowId] = item;
            rows.Add(new DomainConfiguration
            {
                Id = rowId,
                Name = item.Name,
                Domain = typeof(TContract).Name,
                Implementation = OneImplementation,
            });
        }

        var provider = create(GatewayReturning(GenericResult<IEnumerable<DomainConfiguration>>.Success(rows)));

        var implementations = new Mock<IImplementationConfigurationProvider<TContract>>();
        implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(
                byRow.TryGetValue(id, out var item)
                    ? GenericResult<TContract>.Success(item)
                    : GenericResult<TContract>.Success(default!)));

        provider.Register(OneImplementation, implementations.Object);
        return provider;
    }

    private static TProvider Unreadable<TProvider, TContract>(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string reason)
        where TProvider : DomainConfigurationProviderBase<TContract>
        where TContract : IImplementationConfiguration
        => create(GatewayReturning(
            GenericResult<IEnumerable<DomainConfiguration>>.Failure(new GenericMessage(reason))));

    private static IConfigurationGatewayProvider GatewayReturning(
        IGenericResult<IEnumerable<DomainConfiguration>> rows)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return gateways.Object;
    }
}
