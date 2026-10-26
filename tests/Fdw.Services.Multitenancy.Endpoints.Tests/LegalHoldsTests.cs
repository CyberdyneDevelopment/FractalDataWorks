using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the legal holds of an organization's documents (FDW-1575): a document has one standing hold, lifting keeps the record, and
/// only the documents under a standing hold are reported held.
/// </summary>
public sealed class LegalHoldsTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly RetentionTables Tables = new("PlatformConfiguration", "tenant", "RetentionPolicy", "LegalHold");

    private static LegalHoldRow Row(string documentId, bool lifted) => new()
    {
        Id = Guid.NewGuid(), TenantId = Tenant, OrganizationId = Finance, DocumentId = documentId, Reason = "Litigation", PlacedBy = "user-1",
        PlacedAt = DateTimeOffset.UtcNow.AddDays(-3), LiftedBy = lifted ? "user-2" : null, LiftedAt = lifted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
    };

    private static (LegalHolds Service, Mock<IConfigurationGateway> Gateway) Create(params LegalHoldRow[] existing)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<LegalHoldRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<LegalHoldRow>>.Success(existing));
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return (new LegalHolds(gateways.Object, Tables, RetentionTestScope.Organizations().Object, RetentionTestScope.Authority(true).Object, NullLogger<LegalHolds>.Instance), gateway);
    }

    private static void Writes(Mock<IConfigurationGateway> gateway, Times times)
        => gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), times);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentNotHeldIsPutUnderAHoldWithOneWrite()
    {
        var (service, gateway) = Create();

        var placed = await service.Place(Finance, "contracts/acme.pdf", "Litigation", "user-1", TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue();
        placed.Value!.IsHeld.ShouldBeTrue();
        placed.Value.DocumentId.ShouldBe("contracts/acme.pdf");
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentAlreadyHeldCannotBeHeldAgainAndNothingIsWritten()
    {
        var (service, gateway) = Create(Row("contracts/acme.pdf", lifted: false));

        var placed = await service.Place(Finance, "contracts/acme.pdf", "Litigation", "user-1", TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentWhoseHoldWasLiftedCanBeHeldAgain()
    {
        var (service, gateway) = Create(Row("contracts/acme.pdf", lifted: true));

        var placed = await service.Place(Finance, "contracts/acme.pdf", "New claim", "user-1", TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Theory]
    [InlineData("", "reason", "user-1")]
    [InlineData("doc", " ", "user-1")]
    [InlineData("doc", "reason", "")]
    [Trait("Priority", "P0")]
    public async Task AHoldWithoutADocumentAReasonOrAPersonIsRefusedAndNothingIsWritten(string documentId, string reason, string placedBy)
    {
        var (service, gateway) = Create();

        var placed = await service.Place(Finance, documentId, reason, placedBy, TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LiftingAStandingHoldWritesWhoLiftedItAndWhen()
    {
        var (service, gateway) = Create(Row("contracts/acme.pdf", lifted: false));

        var lifted = await service.Lift(Finance, "contracts/acme.pdf", "user-2", TestContext.Current.CancellationToken);

        lifted.IsSuccess.ShouldBeTrue();
        lifted.Value!.IsHeld.ShouldBeFalse();
        lifted.Value.LiftedBy.ShouldBe("user-2");
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LiftingTheHoldOfADocumentNotHeldIsAFailureAndWritesNothing()
    {
        var (service, gateway) = Create(Row("contracts/acme.pdf", lifted: true));

        var lifted = await service.Lift(Finance, "contracts/acme.pdf", "user-2", TestContext.Current.CancellationToken);

        lifted.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OnlyDocumentsUnderAStandingHoldAreReportedHeld()
    {
        var (service, _) = Create(Row("a.pdf", lifted: false), Row("b.pdf", lifted: true));

        var held = await service.HeldDocuments(Finance, TestContext.Current.CancellationToken);

        held.Value!.ShouldBe(["a.pdf"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheListLeavesOutLiftedHoldsWhenOnlyHeldIsAsked()
    {
        var (service, _) = Create(Row("a.pdf", lifted: false), Row("b.pdf", lifted: true));

        var heldOnly = await service.List(Finance, true, TestContext.Current.CancellationToken);
        var all = await service.List(Finance, false, TestContext.Current.CancellationToken);

        heldOnly.Value!.Select(hold => hold.DocumentId).ShouldBe(["a.pdf"]);
        all.Value!.Count.ShouldBe(2);
    }
}
