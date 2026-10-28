using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// The organization scope the retention and feature services are built over in tests (FDW-2215, FDW-2887): one organization in one
/// tenant, which the scope admits or refuses for every right it is asked, and one it does not hold. Which callers it admits is
/// OrganizationScopeTests' question.
/// </summary>
internal static class RetentionTestScope
{
    internal static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    internal static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    internal static readonly Guid Unknown = new("44444444-0000-0000-0000-0000000000ff");

    // Names Finance as an organization of Tenant when it admits the caller, and refuses the caller otherwise; Unknown is refused as not found.
    internal static Mock<IOrganizationScope> Scope(bool allows)
    {
        var scope = new Mock<IOrganizationScope>();
        scope.Setup(s => s.TenantOf(It.IsAny<string>(), Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allows ? GenericResult<Guid>.Success(Tenant) : GenericResult<Guid>.Failure(new GenericMessage("the caller may not act for the organization")));
        scope.Setup(s => s.TenantOf(It.IsAny<string>(), Unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("there is no such organization")));
        return scope;
    }

    // The documents of a department that keeps contracts: they are dated by SignedDate and can be deleted or archived.
    internal static Mock<IRetainedDocuments> Documents()
    {
        var documents = new Mock<IRetainedDocuments>();
        documents.Setup(d => d.Offers(Tenant, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<RetentionOffer>.Success(new RetentionOffer(["SignedDate"], ["Delete", "Archive"])));
        documents.Setup(d => d.DocumentTypes(Tenant, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<string>>.Success(["Contract"]));
        return documents;
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
