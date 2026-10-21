using System;
using System.Collections.Generic;
using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the date rule of a retention policy (FDW-1575): the period counts from the date the administrator designated; a document
/// without that date has no deadline and the upload date never stands in for it.
/// </summary>
public sealed class RetentionClockTests
{
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private static RetentionPolicy SevenYearsFromSigned()
        => new(Guid.NewGuid(), Finance, "Contract", 7, 0, 0, "SignedDate", "Delete");

    [Fact]
    [Trait("Priority", "P0")]
    public void ASevenYearPolicyKeepsADocumentUntilSevenYearsAfterItsSignedDate()
    {
        var dates = new Dictionary<string, DateTimeOffset> { ["SignedDate"] = new(2020, 3, 15, 0, 0, 0, TimeSpan.Zero) };

        var deadline = RetentionClock.DeadlineFor(SevenYearsFromSigned(), dates);

        deadline.IsKnown.ShouldBeTrue();
        deadline.KeepUntil.ShouldBe(new DateTimeOffset(2027, 3, 15, 0, 0, 0, TimeSpan.Zero));
        deadline.MissingStartField.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnAdministratorsMonthsAndDaysAreAddedAfterTheYears()
    {
        var policy = new RetentionPolicy(Guid.NewGuid(), Finance, "Invoice", 1, 6, 10, "EffectiveDate", "Archive");
        var dates = new Dictionary<string, DateTimeOffset> { ["EffectiveDate"] = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) };

        var deadline = RetentionClock.DeadlineFor(policy, dates);

        deadline.KeepUntil.ShouldBe(new DateTimeOffset(2025, 7, 11, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ADocumentWithoutTheDesignatedDateAsksTheUploaderForItAndTheUploadDateIsNotUsed()
    {
        var dates = new Dictionary<string, DateTimeOffset> { ["UploadedDate"] = new(2024, 5, 1, 0, 0, 0, TimeSpan.Zero) };

        var deadline = RetentionClock.DeadlineFor(SevenYearsFromSigned(), dates);

        deadline.IsKnown.ShouldBeFalse();
        deadline.KeepUntil.ShouldBeNull();
        deadline.MissingStartField.ShouldBe("SignedDate");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TheDesignatedFieldIsFoundWhateverItsCase()
    {
        var dates = new Dictionary<string, DateTimeOffset> { ["signeddate"] = new(2020, 3, 15, 0, 0, 0, TimeSpan.Zero) };

        RetentionClock.DeadlineFor(SevenYearsFromSigned(), dates).IsKnown.ShouldBeTrue();
    }
}
