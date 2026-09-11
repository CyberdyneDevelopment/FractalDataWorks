using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Authentication.Tests;

/// <summary>
/// Builds REAL domain configuration providers over a faked configuration store.
/// </summary>
/// <remarks>
/// Why not a mock of the provider: <see cref="DefaultPrincipalResolver"/> takes the providers
/// themselves, which are sealed and whose reads are not virtual, so nothing can stand in for them --
/// a <c>Mock&lt;UserTenantConfigurationProvider&gt;</c> cannot be constructed at all. The catalogue is
/// therefore supplied the way production supplies it: domain rows from the gateway, each dispatched
/// to the implementation provider registered for the row's implementation. A test states only what
/// the STORE holds.
/// <para>
/// The stored items are the CONCRETE configuration types on purpose. The resolver reads through
/// <c>Find&lt;UserTenantImplementationConfiguration&gt;</c> and <c>Find&lt;UserRoleImplementationConfiguration&gt;</c>,
/// which type-test each row, so a row supplied only as its interface would be filtered out and the
/// test would pass for the wrong reason.
/// </para>
/// </remarks>
internal static class ConfigurationCatalog
{
    /// <summary>
    /// The implementation every row in this catalogue is written under.
    /// </summary>
    /// <remarks>
    /// A placeholder is sound here only because the resolver never writes: a read dispatches on
    /// whatever name the row itself carries. A test that exercises a SAVE must name the
    /// implementation production saves under instead, or the write fails to dispatch.
    /// </remarks>
    private const string OneImplementation = "Default";

    /// <summary>The user-to-tenant memberships this store holds.</summary>
    public static UserTenantConfigurationProvider UserTenants(
        IEnumerable<IUserTenantImplementationConfiguration> items)
        => Of<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration>(
            gateways => new UserTenantConfigurationProvider(
                NullLogger<UserTenantConfigurationProvider>.Instance, gateways, "TestStore"), items);

    /// <summary>The user-role assignments this store holds.</summary>
    public static UserRoleConfigurationProvider UserRoles(
        IEnumerable<IUserRoleImplementationConfiguration> items)
        => Of<UserRoleConfigurationProvider, IUserRoleImplementationConfiguration>(
            gateways => new UserRoleConfigurationProvider(
                NullLogger<UserRoleConfigurationProvider>.Instance, gateways, "TestStore"), items);

    /// <summary>A user-role catalogue whose store cannot be read.</summary>
    public static UserRoleConfigurationProvider UnreadableUserRoles(string reason)
        => new(
            NullLogger<UserRoleConfigurationProvider>.Instance,
            GatewayFailing(reason),
            "TestStore");

    /// <summary>The role catalogue this store holds.</summary>
    public static RoleConfigurationProvider Roles(
        IEnumerable<IRoleImplementationConfiguration> items)
        => Of<RoleConfigurationProvider, IRoleImplementationConfiguration>(
            gateways => new RoleConfigurationProvider(
                NullLogger<RoleConfigurationProvider>.Instance, gateways, "TestStore"), items);

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
            // A row's id IS the record's id -- that is what the domain provider stamps back onto the
            // implementation, so minting a separate one would misreport identity. One row per id:
            // appending a second for an id already held returns the same record twice from one read.
            byRow[item.Id] = item;
            rows.RemoveAll(row => row.Id == item.Id);
            rows.Add(new DomainConfiguration
            {
                Id = item.Id,
                Name = item.Name,
                Domain = typeof(TContract).Name,
                Implementation = OneImplementation,
            });
        }

        var provider = create(GatewayReturning(rows));

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

    private static IConfigurationGatewayProvider GatewayFailing(string reason)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DomainConfiguration>>.Failure(new GenericMessage(reason)));

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return gateways.Object;
    }

    private static IConfigurationGatewayProvider GatewayReturning(IEnumerable<DomainConfiguration> rows)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DomainConfiguration>>.Success(rows));

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return gateways.Object;
    }
}
