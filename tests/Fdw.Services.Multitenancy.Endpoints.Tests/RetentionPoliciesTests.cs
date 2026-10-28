using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
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
/// Tests for the retention policies of an organization (FDW-1575): one policy per document type, a period that is empty or negative is
/// refused before anything is written, and a missing policy cannot be removed.
/// </summary>
public sealed class RetentionPoliciesTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly RetentionTables Tables = new("PlatformConfiguration", "tenant", "RetentionPolicy", "LegalHold");

    private static (RetentionPolicies Service, Mock<IConfigurationGateway> Gateway) Create(
        IGenericResult<RetentionOffer>? offers = null, params string[] existingTypes)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<RetentionPolicyRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<RetentionPolicyRow>>.Success(
                [.. existingTypes.Select(type => new RetentionPolicyRow
                {
                    Id = Guid.NewGuid(), TenantId = Tenant, OrganizationId = Finance, DocumentType = type, PeriodYears = 3, StartField = "SignedDate", EndAction = "Delete",
                })]));
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var documents = RetentionTestScope.Documents();
        if (offers is not null)
        {
            documents.Setup(d => d.Offers(Tenant, Finance, It.IsAny<CancellationToken>())).ReturnsAsync(offers);
        }

        return (new RetentionPolicies(gateways.Object, Tables, RetentionTestScope.Scope(true).Object, documents.Object, NullLogger<RetentionPolicies>.Instance), gateway);
    }

    private static void Writes(Mock<IConfigurationGateway> gateway, Times times)
        => gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), times);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASevenYearPolicyIsSetOnceForADocumentTypeThatHasNone()
    {
        var (service, gateway) = Create();

        var set = await service.Set(Finance, new NewRetentionPolicy("Contract", 7, 0, 0, "SignedDate", "Delete"), TestContext.Current.CancellationToken);

        set.IsSuccess.ShouldBeTrue();
        set.Value!.Years.ShouldBe(7);
        set.Value.StartField.ShouldBe("SignedDate");
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SettingAgainTheTypeThatHasAPolicyKeepsItsId()
    {
        var (service, gateway) = Create(existingTypes: "Contract");
        var before = await service.List(Finance, TestContext.Current.CancellationToken);

        var set = await service.Set(Finance, new NewRetentionPolicy("contract", 5, 0, 0, "SignedDate", "Archive"), TestContext.Current.CancellationToken);

        set.IsSuccess.ShouldBeTrue();
        set.Value!.Id.ShouldBe(before.Value![0].Id);
        Writes(gateway, Times.Once());
    }

    [Theory]
    [InlineData("", 1, 0, 0, "SignedDate", "Delete")]
    [InlineData("Contract", 1, 0, 0, " ", "Delete")]
    [InlineData("Contract", 1, 0, 0, "SignedDate", "")]
    [InlineData("Contract", 0, 0, 0, "SignedDate", "Delete")]
    [InlineData("Contract", -1, 3, 0, "SignedDate", "Delete")]
    [Trait("Priority", "P0")]
    public async Task APolicyWithABlankNameOrAnEmptyOrNegativePeriodIsRefusedAndNothingIsWritten(
        string documentType, int years, int months, int days, string startField, string endAction)
    {
        var (service, gateway) = Create();

        var set = await service.Set(Finance, new NewRetentionPolicy(documentType, years, months, days, startField, endAction), TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task RemovingAPolicyTheOrganizationDoesNotHaveIsAFailureAndWritesNothing()
    {
        var (service, gateway) = Create(existingTypes: "Contract");

        var removed = await service.Remove(Finance, "Invoice", TestContext.Current.CancellationToken);

        removed.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task RemovingAPolicyTheOrganizationHasWritesOnce()
    {
        var (service, gateway) = Create(existingTypes: "Contract");

        var removed = await service.Remove(Finance, "Contract", TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AGatewayThatIsDownFailsLoudAndWritesNothing()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Failure(new GenericMessage("the configuration gateway is down")));
        var service = new RetentionPolicies(gateways.Object, Tables, RetentionTestScope.Scope(true).Object, RetentionTestScope.Documents().Object, NullLogger<RetentionPolicies>.Instance);

        var set = await service.Set(Finance, new NewRetentionPolicy("Contract", 7, 0, 0, "SignedDate", "Delete"), TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("ModifiedDate", "Delete", "start field 'ModifiedDate'")]
    [InlineData("SignedDate", "Shred", "end action 'Shred'")]
    [Trait("Priority", "P0")]
    public async Task APolicyThatNamesAStartFieldOrEndActionTheApplicationDoesNotOfferIsRefusedNamingItAndNothingIsWritten(
        string startField, string endAction, string named)
    {
        var (service, gateway) = Create();

        var set = await service.Set(Finance, new NewRetentionPolicy("Contract", 7, 0, 0, startField, endAction), TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
        set.CurrentMessage.ShouldNotBeNull().ShouldContain(named);
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AStartFieldAndEndActionAreMatchedWithoutRegardToCase()
    {
        var (service, gateway) = Create();

        var set = await service.Set(Finance, new NewRetentionPolicy("Contract", 7, 0, 0, "signeddate", "archive"), TestContext.Current.CancellationToken);

        set.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnApplicationThatCannotSayWhatItOffersFailsTheSetLoudAndWritesNothing()
    {
        var (service, gateway) = Create(offers: GenericResult<RetentionOffer>.Failure(new GenericMessage("the store is down")));

        var set = await service.Set(Finance, new NewRetentionPolicy("Contract", 7, 0, 0, "SignedDate", "Delete"), TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }
}
