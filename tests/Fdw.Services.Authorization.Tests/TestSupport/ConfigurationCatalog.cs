using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
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
        => Of<RoleConfigurationProvider, IRoleImplementationConfiguration, RoleDomainConfiguration>(
            gateways => new RoleConfigurationProvider(
                NullLogger<RoleConfigurationProvider>.Instance, gateways, "TestStore", "authz", "Role"), items);

    /// <summary>A role catalogue whose store cannot be read.</summary>
    public static RoleConfigurationProvider UnreadableRoles(string reason)
        => Unreadable<RoleConfigurationProvider, IRoleImplementationConfiguration, RoleDomainConfiguration>(
            gateways => new RoleConfigurationProvider(
                NullLogger<RoleConfigurationProvider>.Instance, gateways, "TestStore", "authz", "Role"), reason);

    /// <summary>The permission catalogue this store holds.</summary>
    public static PermissionConfigurationProvider Permissions(IEnumerable<IPermissionImplementationConfiguration> items)
        => Of<PermissionConfigurationProvider, IPermissionImplementationConfiguration, PermissionDomainConfiguration>(
            gateways => new PermissionConfigurationProvider(
                NullLogger<PermissionConfigurationProvider>.Instance, gateways, "TestStore", "authz", "Permission"), items);

    /// <summary>The role-to-permission grants this store holds.</summary>
    public static RolePermissionConfigurationProvider RolePermissions(IEnumerable<IRolePermissionImplementationConfiguration> items)
        => Of<RolePermissionConfigurationProvider, IRolePermissionImplementationConfiguration, DomainConfiguration>(
            gateways => new RolePermissionConfigurationProvider(
                NullLogger<RolePermissionConfigurationProvider>.Instance, gateways, "TestStore", "authz", "RolePermission"), items);

    /// <summary>The user-role assignments this store holds.</summary>
    public static UserRoleConfigurationProvider UserRoles(IEnumerable<IUserRoleImplementationConfiguration> items)
        => Of<UserRoleConfigurationProvider, IUserRoleImplementationConfiguration, DomainConfiguration>(
            gateways => new UserRoleConfigurationProvider(
                NullLogger<UserRoleConfigurationProvider>.Instance, gateways, "TestStore", "authz", "UserRole"), items);

    /// <summary>A user-role catalogue whose store cannot be read.</summary>
    public static UserRoleConfigurationProvider UnreadableUserRoles(string reason)
        => Unreadable<UserRoleConfigurationProvider, IUserRoleImplementationConfiguration, DomainConfiguration>(
            gateways => new UserRoleConfigurationProvider(
                NullLogger<UserRoleConfigurationProvider>.Instance, gateways, "TestStore", "authz", "UserRole"), reason);

    private static TProvider Of<TProvider, TContract, TDomain>(
        Func<IConfigurationGatewayProvider, TProvider> create,
        IEnumerable<TContract> items)
        where TProvider : DomainConfigurationProviderBase<TContract, TDomain>
        where TContract : IImplementationConfiguration
        where TDomain : DomainConfiguration, new()
    {
        var rows = new List<TDomain>();
        var byRow = new Dictionary<Guid, TContract>();
        foreach (var item in items)
        {
            // The domain row carries the member's own Id, as it does in the store: that Id is the
            // member's identity, the one every junction row (RolePermission.RoleId, UserRole.RoleId)
            // references and the one a read stamps back onto the implementation. A fresh Guid here
            // only ever worked while reads left the implementation's own Id in place; it made every
            // fixture's joins depend on a read that disagreed with the write.
            var rowId = item.Id;
            byRow[rowId] = item;
            var row = new TDomain
            {
                Id = rowId,
                Name = item.Name,
                Domain = typeof(TContract).Name,
                Implementation = OneImplementation,
            };

            // The fields a list shows live on the domain row, copied from the record through the two
            // types' mappers -- the properties the record declares beyond the four every domain has.
            var recordMapper = PocoMapperCollection.ByName(typeof(TDomain).Name);
            var plainNames = PocoMapperCollection.ByName(nameof(DomainConfiguration)).GetPropertyNames();
            var listFields = recordMapper.GetPropertyNames().Where(name => !plainNames.Contains(name, StringComparer.Ordinal)).ToList();
            if (listFields.Count > 0)
            {
                var itemValues = PocoMapperCollection.ByName(item.GetType().Name).MapToParameters(item);
                foreach (var field in listFields)
                    recordMapper.SetValue(row, field, itemValues[field]);
            }

            rows.Add(row);
        }

        var provider = create(GatewayReturning(GenericResult<IEnumerable<TDomain>>.Success(rows)));

        var implementations = new Mock<IImplementationConfigurationProvider<TContract>>();
        implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(
                byRow.TryGetValue(id, out var item)
                    ? GenericResult<TContract>.Success(item)
                    : GenericResult<TContract>.Success(default!)));

        // A list read composes each member through the level-limited read (FDW-951).
        implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) => Task.FromResult(
                byRow.TryGetValue(id, out var item)
                    ? GenericResult<TContract>.Success(item)
                    : GenericResult<TContract>.Success(default!)));

        provider.Register(OneImplementation, implementations.Object);
        return provider;
    }

    private static TProvider Unreadable<TProvider, TContract, TDomain>(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string reason)
        where TProvider : DomainConfigurationProviderBase<TContract, TDomain>
        where TContract : IImplementationConfiguration
        where TDomain : DomainConfiguration, new()
        => create(GatewayReturning(
            GenericResult<IEnumerable<TDomain>>.Failure(new GenericMessage(reason))));

    private static IConfigurationGatewayProvider GatewayReturning<TDomain>(
        IGenericResult<IEnumerable<TDomain>> rows)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<TDomain>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return gateways.Object;
    }
}
