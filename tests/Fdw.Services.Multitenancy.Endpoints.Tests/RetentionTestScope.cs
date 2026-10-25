using System;
using System.Threading;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// The organization and the scope check the retention services are built over in tests (FDW-2215): one organization in one tenant, and
/// a scope authority that allows or refuses every question it is asked.
/// </summary>
internal static class RetentionTestScope
{
    internal static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    internal static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    internal static readonly Guid Unknown = new("44444444-0000-0000-0000-0000000000ff");

    // Reads Finance as an organization of Tenant; Unknown is an organization the store holds nothing for.
    internal static Mock<IOrganizationProvider> Organizations()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Finance, TenantId = Tenant }));
        organizations.Setup(o => o.Get(Unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(null!));
        return organizations;
    }

    // Answers every question the same way: allowed, or refused.
    internal static Mock<IScopeAuthority> Authority(bool allows)
    {
        var authority = new Mock<IScopeAuthority>();
        authority.Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allows ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the caller holds no such right")));
        return authority;
    }
}
