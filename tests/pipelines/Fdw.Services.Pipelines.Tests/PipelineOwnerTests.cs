using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-4573: a new pipeline belongs to the organization the request names, otherwise the one the caller acts in, otherwise a coded refusal; the organization must belong to the
/// tenant the caller acts in and be active; an update cannot move a pipeline to another organization.
/// </summary>
public sealed class PipelineOwnerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherTenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ActingOrg = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid NamedOrg = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private sealed class Accessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed class Fixture
    {
        public Fixture(Guid? actingOrg, Guid? actingTenant = null)
        {
            var caller = new Mock<IAuthenticationContext>();
            caller.SetupGet(c => c.ActiveOrgId).Returns(actingOrg);
            caller.SetupGet(c => c.ActiveTenantId).Returns(actingTenant ?? Tenant);
            Accessor.Current = caller.Object;
            Scope.Setup(s => s.TenantOf("pipelines:write", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, Guid _, CancellationToken _) => ScopeAnswer ?? GenericResult<Guid>.Success(OrganizationTenant));
            Organizations.Setup(o => o.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = id, TenantId = OrganizationTenant, IsActive = OrganizationIsActive }));
        }

        public Accessor Accessor { get; } = new();

        public Mock<IOrganizationScope> Scope { get; } = new();

        public Mock<IOrganizationProvider> Organizations { get; } = new();

        public Guid OrganizationTenant { get; set; } = Tenant;

        public bool OrganizationIsActive { get; set; } = true;

        public IGenericResult<Guid>? ScopeAnswer { get; set; }

        public PipelineOwner Owner => new(Accessor, Scope.Object, Organizations.Object, NullLogger<PipelineOwner>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheOrganizationTheRequestNamesIsUsedBeforeTheOneTheCallerActsIn()
    {
        var fixture = new Fixture(ActingOrg);

        var result = await fixture.Owner.OrganizationOfNew(NamedOrg, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(NamedOrg);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WithNoOrganizationInTheRequestTheOneTheCallerActsInIsUsed()
    {
        var fixture = new Fixture(ActingOrg);

        var result = await fixture.Owner.OrganizationOfNew(null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(ActingOrg);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACallerWhoActsInNoOrganizationAndNamesNoneIsRefusedWithItsOwnCodeAndNothingIsMadeUp()
    {
        var fixture = new Fixture(actingOrg: null);

        var result = await fixture.Owner.OrganizationOfNew(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("PIPELINEOWNER-20006");
        FailureCategory.Of(result).ShouldNotBeNull().HttpStatus.ShouldBe(400);
        fixture.Scope.Verify(s => s.TenantOf(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnOrganizationOfAnotherTenantThanTheOneTheCallerActsInIsRefused()
    {
        var fixture = new Fixture(ActingOrg) { OrganizationTenant = OtherTenant };

        var result = await fixture.Owner.OrganizationOfNew(NamedOrg, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("PIPELINEOWNER-20007");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnOrganizationThatIsNotActiveIsRefused()
    {
        var fixture = new Fixture(ActingOrg) { OrganizationIsActive = false };

        var result = await fixture.Owner.OrganizationOfNew(NamedOrg, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("PIPELINEOWNER-20008");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARefusalOfTheRightAtTheOrganizationIsPassedOnAndTheOrganizationIsNotRead()
    {
        var fixture = new Fixture(ActingOrg) { ScopeAnswer = GenericResult<Guid>.Failure(new GenericMessage("no right at the organization")) };

        var result = await fixture.Owner.OrganizationOfNew(NamedOrg, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no right at the organization");
        fixture.Organizations.Verify(o => o.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnUpdateMayNameNoOrganizationOrTheOneThePipelineHasButNotAnother()
    {
        PipelineOwner.OrganizationUnchanged(NullLogger.Instance, "Daily", ActingOrg, null).IsSuccess.ShouldBeTrue();
        PipelineOwner.OrganizationUnchanged(NullLogger.Instance, "Daily", ActingOrg, ActingOrg).IsSuccess.ShouldBeTrue();
        PipelineOwner.OrganizationUnchanged(NullLogger.Instance, "Daily", null, null).IsSuccess.ShouldBeTrue();

        var moved = PipelineOwner.OrganizationUnchanged(NullLogger.Instance, "Daily", ActingOrg, NamedOrg);
        var named = PipelineOwner.OrganizationUnchanged(NullLogger.Instance, "Daily", null, NamedOrg);

        moved.IsFailure.ShouldBeTrue();
        moved.Messages[^1].Code.ShouldBe("PIPELINEOWNER-20009");
        named.IsFailure.ShouldBeTrue();
    }
}
