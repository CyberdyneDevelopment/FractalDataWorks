using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for what a client can choose when it sets a retention policy or places a legal hold (FDW-2906): the choices and the documents are
/// the application's own, and are read only by a caller who may read the organization's retention.
/// </summary>
public sealed class RetentionCatalogTests
{
    private static readonly Guid Tenant = RetentionTestScope.Tenant;
    private static readonly Guid Finance = RetentionTestScope.Finance;

    private static RetentionCatalog Create(Mock<IRetainedDocuments> documents, bool allows = true)
        => new(documents.Object, RetentionTestScope.Scope(allows).Object, NullLogger<RetentionCatalog>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheChoicesAreWhatTheApplicationOffersTheOrganization()
    {
        var catalog = Create(RetentionTestScope.Documents());

        var choices = await catalog.Choices(Finance, TestContext.Current.CancellationToken);

        choices.IsSuccess.ShouldBeTrue();
        choices.Value!.StartFields.ShouldBe(["SignedDate"]);
        choices.Value.EndActions.ShouldBe(["Delete", "Archive"]);
        choices.Value.DocumentTypes.ShouldBe(["Contract"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoMayNotReadTheOrganizationsRetentionIsRefusedAndTheApplicationIsNotAsked()
    {
        var documents = RetentionTestScope.Documents();
        var catalog = Create(documents, allows: false);

        var choices = await catalog.Choices(Finance, TestContext.Current.CancellationToken);
        var listed = await catalog.Documents(Finance, "Contract", TestContext.Current.CancellationToken);

        choices.IsFailure.ShouldBeTrue();
        listed.IsFailure.ShouldBeTrue();
        documents.Verify(d => d.Offers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        documents.Verify(d => d.DocumentTypes(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        documents.Verify(d => d.List(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheDocumentsOfATypeAreTheOnesTheApplicationListsWithTheirDates()
    {
        var documents = RetentionTestScope.Documents();
        var signed = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        documents.Setup(d => d.List(Tenant, Finance, "Contract", It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<IReadOnlyList<RetainedDocument>>.Success(
                [new RetainedDocument("Contract/a.pdf", "Contract", new Dictionary<string, DateTimeOffset> { ["SignedDate"] = signed })]));
        var catalog = Create(documents);

        var listed = await catalog.Documents(Finance, "Contract", TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value![0].DocumentId.ShouldBe("Contract/a.pdf");
        listed.Value[0].Dates["SignedDate"].ShouldBe(signed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("Priority", "P1")]
    public async Task ADocumentTypeThatIsBlankIsRefusedAndTheApplicationIsNotAsked(string documentType)
    {
        var documents = RetentionTestScope.Documents();
        var catalog = Create(documents);

        var listed = await catalog.Documents(Finance, documentType, TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        documents.Verify(d => d.List(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
