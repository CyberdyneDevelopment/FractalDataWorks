using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-6528: a run started for a person is judged with the tenant provider the reference ETL host and the scheduler really register (the SingleTenant option's), and the
/// real account eligibility. Every earlier test of the judge stood in a tenant provider that answers yes, so none could see that the host's own provider failed every lookup
/// and refused every run for a person.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunPersonUnderSingleTenantTests
{
    private static readonly Guid Tenant = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid GlobalTenant = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid Member = Guid.Parse("66666666-0000-0000-0000-000000000001");
    private static readonly Guid Stranger = Guid.Parse("66666666-0000-0000-0000-000000000002");
    private static readonly Guid GlobalMember = Guid.Parse("66666666-0000-0000-0000-000000000003");

    private readonly List<TenantRow> _tenants =
    [
        new TenantRow { IsCurrent = true, Id = GlobalTenant, Slug = "fdw", DisplayName = "FDW", IsActive = true, IsGlobal = true },
        new TenantRow { IsCurrent = true, Id = Tenant, Slug = "default", DisplayName = "Default", IsActive = true },
    ];

    private readonly List<UserTenantRow> _memberships =
    [
        new UserTenantRow { UserId = Member, TenantId = Tenant, IsCurrent = true },
        new UserTenantRow { UserId = GlobalMember, TenantId = GlobalTenant, IsCurrent = true },
    ];

    // The services a host that runs person runs resolves: the SingleTenant option's tenant provider over an in-memory store, and the real account eligibility.
    private ServiceProvider Services()
    {
        var store = new Mock<IConfigurationGateway>();
        store.Setup(g => g.Execute<IEnumerable<TenantRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken _) => GenericResult<IEnumerable<TenantRow>>.Success(Select(_tenants, command)));
        store.Setup(g => g.Execute<IEnumerable<UserTenantRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken _) => GenericResult<IEnumerable<UserTenantRow>>.Success(Select(_memberships, command)));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(store.Object));

        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(u => u.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid id, CancellationToken _) =>
            GenericResult<IUserImplementationConfiguration>.Success(new UserImplementationConfiguration { Id = id, TenantId = Tenant, IsActive = true }));

        var roles = new Mock<IRoleGrants>();
        roles.Setup(r => r.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(["datasets:read", "datastores:write"]));
        var permissions = new Mock<IUserPermissions>();
        permissions.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(["datasets:read", "datastores:write"], ["Analyst"], null)));

        var services = new ServiceCollection();
        services.AddSingleton(roles.Object);
        services.AddSingleton(permissions.Object);
        services.AddSingleton<IIssuanceEligibility>(new UserAccountEligibility(users.Object, new AuthenticationContextAccessor()));
        services.AddSingleton<ITenantProvider>(new ConfiguredTenantProvider(
            gateways.Object, "PlatformConfiguration", "tenant", "Tenants", "UserTenantsConfiguration", NullLogger<ConfiguredTenantProvider>.Instance));
        return services.BuildServiceProvider();
    }

    private static List<TRow> Select<TRow>(List<TRow> rows, IDataCommand command)
    {
        var conditions = Conditions(((QueryCommand<TRow>)command).Filter?.Root).ToList();
        return [.. rows.Where(row => conditions.All(c => Equals(typeof(TRow).GetProperty(c.PropertyName)!.GetValue(row), c.Value)))];
    }

    private static IEnumerable<IFilterCondition> Conditions(IFilterNode? node)
    {
        if (node is IFilterCondition condition)
        {
            yield return condition;
        }
        else if (node?.GetType().GetProperty("Nodes")?.GetValue(node) is IEnumerable children)
        {
            foreach (var child in children.OfType<IFilterNode>())
            {
                foreach (var inner in Conditions(child))
                    yield return inner;
            }
        }
    }

    private static PipelineExecutionRequest Request(Guid person, Guid? tenant = null) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "NightlyLoad",
        TriggerSource = "Api",
        RunRoles = ["DataWriter"],
        TenantId = tenant ?? Tenant,
        PersonId = person
    };

    private static Task<IGenericResult<IAuthenticationContext>> Judge(ServiceProvider services, PipelineExecutionRequest request)
        => PipelineRequestProcessor.ResolveRunIdentity(services, request, NullLogger.Instance, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMemberOfTheTenantRunsAsThemselvesUnderTheSingleTenantProvider()
    {
        using var services = Services();

        var result = await Judge(services, Request(Member));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().UserId.ShouldBe(Member.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMemberOfTheGlobalTenantRunsInAnyTenant()
    {
        using var services = Services();

        var result = await Judge(services, Request(GlobalMember));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoIsNotAMemberIsRefusedAsRunPersonIneligible()
    {
        using var services = Services();

        var result = await Judge(services, Request(Stranger));

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100833");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunQueuedByAMemberWhoseMembershipIsRemovedBeforeItIsPickedUpIsRefusedWhenJudged()
    {
        using var services = Services();
        var queued = Request(Member);
        (await Judge(services, queued)).IsSuccess.ShouldBeTrue();

        _memberships.Single(m => m.UserId == Member).IsCurrent = false;

        var result = await Judge(services, queued);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100833");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunInATenantWithNoRowIsRefusedAndIsNotAnAnswerOfYes()
    {
        using var services = Services();

        var result = await Judge(services, Request(GlobalMember, Guid.Parse("11111111-1111-1111-1111-111111111111")));

        result.IsFailure.ShouldBeTrue();
    }
}
