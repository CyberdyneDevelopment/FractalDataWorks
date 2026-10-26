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
/// Tests for the endpoints over retention (FDW-1575): each policy, hold, release and document ended is recorded in the audit at the
/// organization level naming the organization, and a change that cannot be recorded answers 500. Who may act is the services' to decide (FDW-2215).
/// </summary>
[Trait("Category", "Security")]
public sealed class RetentionEndpointTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private sealed class Place(ILegalHolds holds, IAdministrativeAudit audit)
        : PlaceLegalHoldEndpointBase(NullLogger.Instance, holds, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SetPolicy(IRetentionPolicies policies, IAdministrativeAudit audit)
        : SetRetentionPolicyEndpointBase(NullLogger.Instance, policies, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Run(IRetentionKeeper keeper, IAdministrativeAudit audit)
        : RunRetentionEndpointBase(NullLogger.Instance, keeper, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> ActingIn(Guid tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString()), new Claim(ClaimDefinitions.sub.Name, "user-1")], "test"));

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static Mock<ILegalHolds> Holds()
    {
        var holds = new Mock<ILegalHolds>();
        holds.Setup(h => h.Place(Finance, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<LegalHold>.Success(new LegalHold(Guid.NewGuid(), Finance, "contracts/acme.pdf", "Litigation", "user-1", DateTimeOffset.UtcNow, null, null)));
        return holds;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlacedHoldIsRecordedAtTheOrganizationLevelWithItsReasonAndTheCaller()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var holds = Holds();
        var endpoint = Factory.Create<Place>(ActingIn(Tenant), holds.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new PlaceLegalHoldRequest { OrganizationId = Finance, DocumentId = "contracts/acme.pdf", Reason = "Litigation" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        holds.Verify(h => h.Place(Finance, "contracts/acme.pdf", "Litigation", "user-1", It.IsAny<CancellationToken>()), Times.Once);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("LegalHoldPlaced");
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Finance);
        entry.Reason.ShouldBe("Litigation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AHoldPlacedButNotRecordedIsAnswered500()
    {
        var endpoint = Factory.Create<Place>(ActingIn(Tenant), Holds().Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(new PlaceLegalHoldRequest { OrganizationId = Finance, DocumentId = "a.pdf", Reason = "Litigation" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASetPolicyIsRecordedAtTheOrganizationLevelNamingTheDocumentType()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var policies = new Mock<IRetentionPolicies>();
        policies.Setup(p => p.Set(Finance, It.IsAny<NewRetentionPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<RetentionPolicy>.Success(new RetentionPolicy(Guid.NewGuid(), Finance, "Contract", 7, 0, 0, "SignedDate", "Delete")));
        var endpoint = Factory.Create<SetPolicy>(ActingIn(Tenant), policies.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(
            new SetRetentionPolicyRequest { OrganizationId = Finance, DocumentType = "Contract", Years = 7, StartField = "SignedDate", EndAction = "Delete" },
            CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("RetentionPolicySet");
        entry.ItemId.ShouldBe($"{Finance}:Contract");
        entry.OrganizationId.ShouldBe(Finance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task EveryDocumentTheRunEndsIsRecordedInTheAudit()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var keeper = new Mock<IRetentionKeeper>();
        keeper.Setup(k => k.Run(Finance, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<RetentionRunReport>.Success(new RetentionRunReport(
                DateTimeOffset.UtcNow, 3, 1, 0,
                [new RetentionOutcome("a.pdf", "Contract", "Delete", null), new RetentionOutcome("b.pdf", "Contract", "Delete", null)], [])));
        var endpoint = Factory.Create<Run>(ActingIn(Tenant), keeper.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationRetentionRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Ended.Count.ShouldBe(2);
        recorded.Count.ShouldBe(2);
        recorded.ShouldAllBe(entry => entry.ChangeKind == "DocumentRetentionEnded" && entry.OrganizationId == Finance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARunWhoseEndedDocumentsCannotBeRecordedIsAnswered500()
    {
        var keeper = new Mock<IRetentionKeeper>();
        keeper.Setup(k => k.Run(Finance, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<RetentionRunReport>.Success(new RetentionRunReport(
                DateTimeOffset.UtcNow, 1, 0, 0, [new RetentionOutcome("a.pdf", "Contract", "Delete", null)], [])));
        var endpoint = Factory.Create<Run>(ActingIn(Tenant), keeper.Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(new OrganizationRetentionRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }
}
