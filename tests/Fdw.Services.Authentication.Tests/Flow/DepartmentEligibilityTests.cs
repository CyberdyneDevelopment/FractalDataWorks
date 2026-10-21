using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Steps;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Tests for the eligibility of a token for a department (FDW-1802): the tenant provider handed to the run answers whether the person may
/// act in the department, a refusal denies the token, a question that cannot be answered fails loud, and no department stated is the
/// ordinary token that asks nothing about departments.
/// </summary>
public sealed class DepartmentEligibilityTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private sealed class Rig
    {
        public Principal Principal { get; } = new() { Id = Guid.NewGuid(), TenantId = Tenant, OrganizationId = Finance };

        public Mock<ITenantProvider> Tenants { get; } = new();

        private readonly Mock<IUserConfigurationProvider> _users = new();

        public Rig(IGenericResult<bool> departmentAccess)
        {
            _users.Setup(u => u.Get(Principal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(
                GenericResult<IUserImplementationConfiguration>.Success(new UserImplementationConfiguration { Id = Principal.Id, TenantId = Tenant, IsActive = true }));
            Tenants.Setup(t => t.ValidateTenantAccess(Tenant, Principal.Id.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<bool>.Success(true));
            Tenants.Setup(t => t.ValidateOrganizationAccess(Tenant, Finance, Principal.Id.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(departmentAccess);
        }

        public Task<IGenericResult<Decision>> Decide(bool stateDepartment = true)
            => new UserAccountEligibility(_users.Object, new AuthenticationContextAccessor())
                .MayBeIssued(stateDepartment ? Principal : Principal with { OrganizationId = null }, Tenants.Object, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoMayActInTheDepartmentMayBeIssuedATokenForIt()
    {
        var decision = await new Rig(GenericResult<bool>.Success(true)).Decide();

        decision.IsSuccess.ShouldBeTrue();
        decision.Value!.Permitted.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoMayNotActInTheDepartmentIsRefused()
    {
        var decision = await new Rig(GenericResult<bool>.Success(false)).Decide();

        decision.IsSuccess.ShouldBeTrue();
        decision.Value!.Permitted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADepartmentQuestionThatCannotBeAnsweredFailsLoudInsteadOfIssuing()
    {
        var decision = await new Rig(GenericResult<bool>.Failure(new GenericMessage("the departments could not be read"))).Decide();

        decision.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task NoDepartmentStatedIsTheOrdinaryTokenAndAsksNothingAboutDepartments()
    {
        var rig = new Rig(GenericResult<bool>.Success(false));

        var decision = await rig.Decide(stateDepartment: false);

        decision.Value!.Permitted.ShouldBeTrue();
        rig.Tenants.Verify(
            t => t.ValidateOrganizationAccess(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
