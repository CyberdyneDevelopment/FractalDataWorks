using System;
using System.Linq;
using Fdw.Services.Data.DataSets;
using Fdw.Services.Descriptions.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-6208: the history of a data set field shows the text each version had, read from the descriptions table beside the field's own versions.</summary>
public sealed class DescriptionHistoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(int seconds) => Start.AddSeconds(seconds);

    private static DescriptionVersion Text(string text, int seconds, bool deleted = false) => new(text, At(seconds), deleted);

    [Fact]
    [Trait("Priority", "P0")]
    public void ADescriptionWrittenJustAfterAVersionBelongsToThatVersionAndNotToTheOneBefore()
    {
        // Field saved at 0, 10, 20; the description is written a moment after each save that changed it.
        var texts = DescriptionHistory.TextsOf([At(0), At(10), At(20)], [Text("first", 1), Text("second", 11)]);

        texts.ShouldBe(["first", "second", "second"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AVersionBeforeAnyDescriptionAndAThingThatNeverHadOneHaveNoText()
    {
        DescriptionHistory.TextsOf([At(0), At(10)], [Text("later", 11)]).ShouldBe([string.Empty, "later"]);
        DescriptionHistory.TextsOf([At(0), At(10)], []).ShouldBe([string.Empty, string.Empty]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ARemovedDescriptionReadsAsNoTextFromTheVersionItWasRemovedAt()
    {
        var texts = DescriptionHistory.TextsOf([At(0), At(10)], [Text("kept", 1), Text("kept", 11, deleted: true)]);

        texts.ShouldBe(["kept", string.Empty]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void NoVersionsOfTheThingGiveNoTexts() => DescriptionHistory.TextsOf([], [Text("x", 1)]).ShouldBeEmpty();
}
