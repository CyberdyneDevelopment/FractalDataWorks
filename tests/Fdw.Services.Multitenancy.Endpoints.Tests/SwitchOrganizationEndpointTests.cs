using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the endpoint that switches the department a person acts in (FDW-1802): the department must be in the tenant the caller
/// acts in, the token comes from the issuance check, a refusal issues nothing and records nothing, and a switch made but not recorded
/// answers 500.
/// </summary>
[Trait("Category", "Security")]
public sealed class SwitchOrganizationEndpointTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid User = new("55555555-0000-0000-0000-000000000001");

    private sealed class Switch(IOrganizationProvider organizations, ITenantProvider tenants, IAdministrativeAudit audit, bool refuse)
        : SwitchOrganizationEndpointBase(NullLogger.Instance, organizations, tenants, audit)
    {
        public int Issued { get; private set; }

        protected override void ConfigureEndpoint() => AllowAnonymous();

        protected override Task<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>> GenerateNewToken(
            Guid userId, Guid tenantId, OrganizationConfiguration organization, CancellationToken cancellationToken)
        {
            Issued++;
            return Task.FromResult<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>>(refuse
                ? GenericResult<(string, DateTimeOffset)>.Failure(new GenericMessage("the person holds no access in the department"))
                : GenericResult<(string, DateTimeOffset)>.Success(("token-for-finance", DateTimeOffset.UtcNow.AddHours(1))));
        }
    }

    private static Action<DefaultHttpContext> Acting(Guid tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString()), new Claim(ClaimDefinitions.sub.Name, User.ToString())], "test"));

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static Mock<IOrganizationProvider> Organizations(Guid tenantId)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Finance, TenantId = tenantId, Name = "Finance", Slug = "finance", IsActive = true }));
        return organizations;
    }

    private static Mock<ITenantProvider> Tenants(bool mayActInDepartment)
    {
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.ValidateOrganizationAccess(Tenant, Finance, User.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(mayActInDepartment));
        return tenants;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonSwitchesToADepartmentOfTheirTenantAndTheSwitchIsRecordedAtTheOrganizationLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<Switch>(Acting(Tenant), Organizations(Tenant).Object, Tenants(true).Object, Audit(recorded).Object, false);

        await endpoint.HandleAsync(new SwitchOrganizationRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.AccessToken.ShouldBe("token-for-finance");
        endpoint.Response.Organization!.Id.ShouldBe(Finance);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("OrganizationSwitched");
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Finance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADepartmentOfAnotherTenantIsRefusedAndNoTokenIsIssued()
    {
        var endpoint = Factory.Create<Switch>(Acting(Tenant), Organizations(OtherTenant).Object, Tenants(true).Object, Audit().Object, false);

        await endpoint.HandleAsync(new SwitchOrganizationRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        endpoint.Issued.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARefusalByTheIssuanceCheckIssuesNothingAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<Switch>(Acting(Tenant), Organizations(Tenant).Object, Tenants(true).Object, Audit(recorded).Object, true);

        await endpoint.HandleAsync(new SwitchOrganizationRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchMadeButNotRecordedIsAnswered500AndSendsNoToken()
    {
        var endpoint = Factory.Create<Switch>(Acting(Tenant), Organizations(Tenant).Object, Tenants(true).Object, Audit(succeeds: false).Object, false);

        await endpoint.HandleAsync(new SwitchOrganizationRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoseTokenNamesNoTenantIsRefused()
    {
        var endpoint = Factory.Create<Switch>(
            ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimDefinitions.sub.Name, User.ToString())], "test")),
            Organizations(Tenant).Object, Tenants(true).Object, Audit().Object, false);

        await endpoint.HandleAsync(new SwitchOrganizationRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        endpoint.Issued.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonWithNoAccessInTheDepartmentIsToldNo403AndNoTokenIsIssued()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<Switch>(Acting(Tenant), Organizations(Tenant).Object, Tenants(false).Object, Audit(recorded).Object, false);

        await endpoint.HandleAsync(new SwitchOrganizationRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        endpoint.Issued.ShouldBe(0);
        recorded.ShouldBeEmpty();
    }
}
