using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests that the retention services decide who may act inside the service (FDW-2215): each method asks the scope authority for its right at
/// the organization's own scope before it touches the store, an organization the store does not hold is refused in the forbidden category,
/// and a refused caller reads and writes nothing.
/// </summary>
[Trait("Category", "Security")]
public sealed class RetentionScopeTests
{
    private static readonly Guid Tenant = RetentionTestScope.Tenant;
    private static readonly Guid Finance = RetentionTestScope.Finance;
    private static readonly Guid Unknown = RetentionTestScope.Unknown;
    private static readonly RetentionTables Tables = new("PlatformConfiguration", "tenant", "RetentionPolicy", "LegalHold");

    private sealed class Rig(bool allows)
    {
        internal Mock<IConfigurationGatewayProvider> Gateways { get; } = new();

        internal Mock<IScopeAuthority> Authority { get; } = RetentionTestScope.Authority(allows);

        internal RetentionPolicies Policies => new(Gateways.Object, Tables, RetentionTestScope.Organizations().Object, Authority.Object, NullLogger<RetentionPolicies>.Instance);

        internal LegalHolds Holds => new(Gateways.Object, Tables, RetentionTestScope.Organizations().Object, Authority.Object, NullLogger<LegalHolds>.Instance);

        internal RetentionKeeper Keeper => new(
            Policies, Holds, new Mock<IRetainedDocuments>().Object, RetentionTestScope.Organizations().Object, Authority.Object,
            NullLogger<RetentionKeeper>.Instance);

        internal void NothingRead() => Gateways.Verify(g => g.Get(It.IsAny<string>()), Times.Never);

        internal void Asked(string right) => Authority.Verify(a => a.May(right, new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static readonly NewRetentionPolicy Policy = new("Contract", 7, 0, 0, "SignedDate", "Delete");

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReaderOfPoliciesIsAskedForTheReadRightAtTheOrganization()
    {
        var rig = new Rig(false);

        var listed = await rig.Policies.List(Finance, TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        rig.Asked("retention:read");
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASetOrRemoveOfAPolicyIsAskedForTheWriteRightAtTheOrganization()
    {
        var rig = new Rig(false);

        var set = await rig.Policies.Set(Finance, Policy, TestContext.Current.CancellationToken);
        var removed = await rig.Policies.Remove(Finance, "Contract", TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
        removed.IsFailure.ShouldBeTrue();
        rig.Authority.Verify(a => a.May("retention:write", new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Exactly(2));
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlaceOrLiftOfAHoldIsAskedForTheLegalHoldsWriteRightAndAReadForTheLegalHoldsReadRight()
    {
        var rig = new Rig(false);

        var placed = await rig.Holds.Place(Finance, "a.pdf", "Litigation", "user-1", TestContext.Current.CancellationToken);
        var lifted = await rig.Holds.Lift(Finance, "a.pdf", "user-1", TestContext.Current.CancellationToken);
        var listed = await rig.Holds.List(Finance, false, TestContext.Current.CancellationToken);
        var held = await rig.Holds.HeldDocuments(Finance, TestContext.Current.CancellationToken);

        new IGenericResult[] { placed, lifted, listed, held }.ShouldAllBe(result => result.IsFailure);
        rig.Authority.Verify(a => a.May("legal-holds:write", new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Exactly(2));
        rig.Authority.Verify(a => a.May("legal-holds:read", new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Exactly(2));
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARunIsAskedForTheRetentionWriteRightAndAChallengedCallerDoesNotReachTheDocuments()
    {
        var rig = new Rig(false);

        var report = await rig.Keeper.Run(Finance, DateTimeOffset.UtcNow, TestContext.Current.CancellationToken);

        report.IsFailure.ShouldBeTrue();
        rig.Asked("retention:write");
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationTheStoreDoesNotHoldIsRefusedInTheForbiddenCategoryWithoutAskingTheAuthority()
    {
        var rig = new Rig(true);

        var listed = await rig.Policies.List(Unknown, TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        FailureCategory.Of(listed).ShouldBe(ResultCategories.ByName("Forbidden"));
        rig.Authority.Verify(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.NothingRead();
    }
}
