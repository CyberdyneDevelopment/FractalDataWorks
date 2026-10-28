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
/// Tests that the retention services decide who may act inside the service (FDW-2215, FDW-2887): each method asks the organization scope for its
/// right for the organization before it touches the store, and a refused caller reads and writes nothing. What makes the scope refuse, and
/// that it refuses another company's organization as it refuses a missing one, is OrganizationScopeTests' question.
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

        internal Mock<IOrganizationScope> Scope { get; } = RetentionTestScope.Scope(allows);

        internal RetentionPolicies Policies => new(Gateways.Object, Tables, Scope.Object, RetentionTestScope.Documents().Object, NullLogger<RetentionPolicies>.Instance);

        internal LegalHolds Holds => new(Gateways.Object, Tables, Scope.Object, NullLogger<LegalHolds>.Instance);

        internal RetentionKeeper Keeper => new(
            Policies, Holds, new Mock<IRetainedDocuments>().Object, Scope.Object,
            NullLogger<RetentionKeeper>.Instance);

        internal void NothingRead() => Gateways.Verify(g => g.Get(It.IsAny<string>()), Times.Never);

        internal void Asked(string right) => Scope.Verify(s => s.TenantOf(right, Finance, It.IsAny<CancellationToken>()), Times.Once);
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
        rig.Scope.Verify(s => s.TenantOf("retention:write", Finance, It.IsAny<CancellationToken>()), Times.Exactly(2));
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
        rig.Scope.Verify(s => s.TenantOf("legal-holds:write", Finance, It.IsAny<CancellationToken>()), Times.Exactly(2));
        rig.Scope.Verify(s => s.TenantOf("legal-holds:read", Finance, It.IsAny<CancellationToken>()), Times.Exactly(2));
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
}
