using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-6528: the single-tenant option answers tenant facts from the configuration store, so a run started for a person on the ETL host or the scheduler can be judged.
/// Before it, the null provider failed every lookup and every such run was refused. The store here is a small in-memory one that applies the filters a query carries,
/// so a membership that is not current, or a row that is deleted, is left out the way the real store leaves it out.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfiguredTenantProviderTests
{
    private static readonly Guid Global = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid Default = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid Inactive = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Missing = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Member = Guid.Parse("66666666-0000-0000-0000-000000000001");
    private static readonly Guid Stranger = Guid.Parse("66666666-0000-0000-0000-000000000002");
    private static readonly Guid GlobalMember = Guid.Parse("66666666-0000-0000-0000-000000000003");

    private readonly List<TenantRow> _tenants =
    [
        new TenantRow { IsCurrent = true, Id = Global, Slug = "fdw", DisplayName = "FDW", IsActive = true, IsGlobal = true },
        new TenantRow { IsCurrent = true, Id = Default, Slug = "default", DisplayName = "Default", IsActive = true },
        new TenantRow { IsCurrent = true, Id = Inactive, Slug = "old", DisplayName = "Old", IsActive = false },
    ];

    private readonly List<UserTenantRow> _memberships =
    [
        new UserTenantRow { UserId = Member, TenantId = Default, IsCurrent = true },
        new UserTenantRow { UserId = GlobalMember, TenantId = Global, IsCurrent = true },
    ];

    private ConfiguredTenantProvider Provider(IGenericResult<IConfigurationGateway>? gateway = null)
    {
        var store = new Mock<IConfigurationGateway>();
        store.Setup(g => g.Execute<IEnumerable<TenantRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken _) => GenericResult<IEnumerable<TenantRow>>.Success(Select(_tenants, command)));
        store.Setup(g => g.Execute<IEnumerable<UserTenantRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken _) => GenericResult<IEnumerable<UserTenantRow>>.Success(Select(_memberships, command)));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(gateway ?? GenericResult<IConfigurationGateway>.Success(store.Object));
        return new ConfiguredTenantProvider(gateways.Object, "ConfigurationDb", "tenant", "Tenants", "UserTenantsConfiguration", NullLogger<ConfiguredTenantProvider>.Instance);
    }

    // The rows a query's equality conditions select, the way the store selects them.
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

    private async Task<bool> MayAct(Guid person, Guid tenant)
    {
        var answer = await Provider().ValidateTenantAccess(tenant, person.ToString(), TestContext.Current.CancellationToken);
        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        return answer.Value;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ASingleTenantHostRegistersTheConfiguredTenantProviderAndNotTheNullOne()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton(Mock.Of<IConfigurationGatewayProvider>());
        builder.Services.AddLogging();

        new SingleTenantMultitenancyType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantProvider>().ShouldBeOfType<ConfiguredTenantProvider>();
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMemberMayActInTheirTenant() => (await MayAct(Member, Default)).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoIsNotAMemberMayNotAct() => (await MayAct(Stranger, Default)).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMemberOfTheGlobalTenantMayActInAnyActiveTenant() => (await MayAct(GlobalMember, Default)).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMembershipRemovedAfterARunWasQueuedIsRefusedWhenTheRunIsJudged()
    {
        (await MayAct(Member, Default)).ShouldBeTrue();

        _memberships.Single(m => m.UserId == Member).IsCurrent = false;

        (await MayAct(Member, Default)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADeletedMembershipIsNotAMembership()
    {
        _memberships.Single(m => m.UserId == Member).IsDeleted = true;

        (await MayAct(Member, Default)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NobodyMayActInAnInactiveTenant()
    {
        _memberships.Add(new UserTenantRow { UserId = Member, TenantId = Inactive, IsCurrent = true });

        (await MayAct(Member, Inactive)).ShouldBeFalse();
        (await MayAct(GlobalMember, Inactive)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantWithNoRowIsNotActedInEvenByAGlobalMember() => (await MayAct(GlobalMember, Missing)).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADeletedTenantIsNotActedIn()
    {
        _tenants.Single(t => t.Id == Default).IsDeleted = true;

        (await MayAct(Member, Default)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ThePersonMustBeAnIdAndSaysSoWithItsOwnCode()
    {
        var answer = await Provider().ValidateTenantAccess(Default, "not-an-id", TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Messages[^1].Code.ShouldBe("TENANTLOOKUP-30003");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreThatCannotBeReachedIsADependencyFailureAndNeverAnAnswerOfNo()
    {
        var provider = Provider(GenericResult<IConfigurationGateway>.Failure(new Fdw.Messages.GenericMessage("the store is down")));

        var answer = await provider.ValidateTenantAccess(Default, Member.ToString(), TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Messages[^1].Code.ShouldBe("TENANTLOOKUP-60001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheGlobalTenantIsKnownAsGlobalAndAnOrdinaryOneIsNot()
    {
        var provider = Provider();

        (await provider.GetTenant(Global, TestContext.Current.CancellationToken)).Value!.IsGlobal.ShouldBeTrue();
        (await provider.GetTenant(Default, TestContext.Current.CancellationToken)).Value!.IsGlobal.ShouldBeFalse();
        (await provider.GetTenantBySlug("default", TestContext.Current.CancellationToken)).Value!.Id.ShouldBe(Default);
        (await provider.GetActiveTenants(TestContext.Current.CancellationToken)).Value!.Select(t => t.Id).ShouldBe([Global, Default], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantTheStoreDoesNotHoldIsNotFoundWithItsOwnCode()
    {
        var answer = await Provider().GetTenant(Missing, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Messages[^1].Code.ShouldBe("TENANTLOOKUP-30001");
        FailureCategory.IsMissing(answer).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AQuestionTheOptionDoesNotAnswerSaysSoAndNeverAnswersYes()
    {
        var answer = await Provider().ValidateOrganizationAccess(Default, Guid.NewGuid(), Member.ToString(), TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Messages[^1].Code.ShouldBe("TENANTLOOKUP-60002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ThisProvidersAnswerIsTheSharedRulesAnswerOverTheSameRows()
    {
        // One rule, asked by every provider that answers from rows: this provider's answer for every person and tenant is the rule's own answer over the same rows.
        var facts = _tenants.Select(t => new TenantFact(t.Id, t.IsActive, t.IsGlobal)).ToList();
        foreach (var person in new[] { Member, Stranger, GlobalMember })
        {
            var memberOf = _memberships.Where(m => m.UserId == person).Select(m => m.TenantId).ToHashSet();
            foreach (var tenant in new[] { Global, Default, Inactive, Missing })
                (await MayAct(person, tenant)).ShouldBe(TenantAccessRule.Allows(tenant, facts, memberOf), $"{person} in {tenant}");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASingleTenantHostWhoseSchemaLacksTheTenantContainersDoesNotStartAndNamesEachOne()
    {
        var host = new HostApplicationBuilder();
        host.Services.AddSingleton(new Fdw.Services.Data.Configuration.ConfigurationSchema());
        using var app = host.Build();

        var result = new SingleTenantMultitenancyType().Initialize(app, NullLoggerFactory.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldBe(["TENANTLOOKUP-60003", "TENANTLOOKUP-60003", "TENANTLOOKUP-60003"]);
        result.Messages.Select(m => m.Message).ShouldContain(m => m.Contains("'Tenants'", StringComparison.Ordinal));
        result.Messages.Select(m => m.Message).ShouldContain(m => m.Contains("'UserTenantsConfiguration'", StringComparison.Ordinal));
        result.Messages.Select(m => m.Message).ShouldContain(m => m.Contains("'Organization'", StringComparison.Ordinal));
    }
}
