using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the retention job (FDW-1575): a document past its deadline and not held is ended, a held one is left, a document without
/// the date its policy counts from has no deadline, and a document that cannot be ended is reported while the run goes on.
/// </summary>
public sealed class RetentionKeeperTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static RetainedDocument Document(string id, DateTimeOffset? signed)
        => new(id, "Contract", signed is { } date ? new Dictionary<string, DateTimeOffset> { ["SignedDate"] = date } : new Dictionary<string, DateTimeOffset>());

    private static (RetentionKeeper Keeper, Mock<IRetainedDocuments> Documents) Create(
        RetainedDocument[] documents, string[] held, string[]? failToEnd = null, bool listingFails = false)
    {
        var policies = new Mock<IRetentionPolicies>();
        policies.Setup(p => p.List(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<IReadOnlyList<RetentionPolicy>>.Success([new RetentionPolicy(Guid.NewGuid(), Finance, "Contract", 7, 0, 0, "SignedDate", "Delete")]));
        var holds = new Mock<ILegalHolds>();
        holds.Setup(h => h.HeldDocuments(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISet<string>>.Success(new HashSet<string>(held, StringComparer.Ordinal)));
        var retained = new Mock<IRetainedDocuments>();
        retained.Setup(d => d.List(Tenant, Finance, "Contract", It.IsAny<CancellationToken>())).ReturnsAsync(listingFails
            ? GenericResult<IReadOnlyList<RetainedDocument>>.Failure(new GenericMessage("the store is down"))
            : GenericResult<IReadOnlyList<RetainedDocument>>.Success(documents));
        retained.Setup(d => d.End(Tenant, Finance, It.IsAny<RetainedDocument>(), "Delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, RetainedDocument document, string _, CancellationToken _) =>
                failToEnd is not null && failToEnd.Contains(document.DocumentId, StringComparer.Ordinal)
                    ? GenericResult.Failure(new GenericMessage("the file is locked"))
                    : GenericResult.Success());
        return (new RetentionKeeper(policies.Object, holds.Object, retained.Object, RetentionTestScope.Organizations().Object, RetentionTestScope.Authority(true).Object, NullLogger<RetentionKeeper>.Instance), retained);
    }

    private static void EndedNever(Mock<IRetainedDocuments> documents, string id)
        => documents.Verify(d => d.End(It.IsAny<Guid>(), It.IsAny<Guid>(), It.Is<RetainedDocument>(document => document.DocumentId == id), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentPastItsSevenYearsAndNotHeldIsEnded()
    {
        var (keeper, _) = Create([Document("old.pdf", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))], []);

        var report = await keeper.Run(Finance, Now, TestContext.Current.CancellationToken);

        report.IsSuccess.ShouldBeTrue();
        report.Value!.Ended.Select(outcome => outcome.DocumentId).ShouldBe(["old.pdf"]);
        report.Value.Held.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentPastItsDeadlineButUnderAHoldSurvivesTheRun()
    {
        var (keeper, documents) = Create([Document("held.pdf", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))], ["held.pdf"]);

        var report = await keeper.Run(Finance, Now, TestContext.Current.CancellationToken);

        report.Value!.Ended.ShouldBeEmpty();
        report.Value.Held.ShouldBe(1);
        EndedNever(documents, "held.pdf");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentStillInsideItsPeriodIsLeftAlone()
    {
        var (keeper, documents) = Create([Document("new.pdf", new DateTimeOffset(2028, 1, 1, 0, 0, 0, TimeSpan.Zero))], []);

        var report = await keeper.Run(Finance, Now, TestContext.Current.CancellationToken);

        report.Value!.Considered.ShouldBe(1);
        report.Value.Ended.ShouldBeEmpty();
        EndedNever(documents, "new.pdf");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentWithoutItsStartDateHasNoDeadlineAndIsNeverEnded()
    {
        var (keeper, documents) = Create([Document("undated.pdf", null)], []);

        var report = await keeper.Run(Finance, Now, TestContext.Current.CancellationToken);

        report.Value!.Undated.ShouldBe(1);
        report.Value.Ended.ShouldBeEmpty();
        EndedNever(documents, "undated.pdf");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentThatCannotBeEndedIsReportedAndTheRunGoesOn()
    {
        var old = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (keeper, _) = Create([Document("locked.pdf", old), Document("free.pdf", old)], [], failToEnd: ["locked.pdf"]);

        var report = await keeper.Run(Finance, Now, TestContext.Current.CancellationToken);

        report.Value!.Failed.Select(outcome => outcome.DocumentId).ShouldBe(["locked.pdf"]);
        report.Value.Failed[0].Reason.ShouldNotBeNull();
        report.Value.Ended.Select(outcome => outcome.DocumentId).ShouldBe(["free.pdf"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AListingThatFailsFailsTheRunLoudInsteadOfReportingNothingEnded()
    {
        var (keeper, _) = Create([], [], listingFails: true);

        var report = await keeper.Run(Finance, Now, TestContext.Current.CancellationToken);

        report.IsFailure.ShouldBeTrue();
    }
}
