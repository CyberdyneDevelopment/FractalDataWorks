using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Descriptions.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

// A description of several parts: the service writes them in one unit, replaces all of them on a save, and reads them back joined in one query (FDW-6123).
public sealed partial class DescriptionServiceTests
{
    private const string Long = "alpha beta gamma delta";

    private static DescriptionDomainConfiguration Held(string type, Guid thing, string language, int ordinal, string text = "part")
        => new()
        {
            Id = Guid.NewGuid(), Name = $"{type}/Text/{thing:D}/{language}/{ordinal}", DescriptionType = type, DescriptionSubType = DescriptionTextSubType.OptionName,
            ThingId = thing, Language = language, Ordinal = ordinal, ThingName = "Orders", Text = text,
        };

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SaveOfALongTextWritesEachPartAsARowOfItsOwnInOneUnitAndTheTextJoinedIsTheTextWritten()
    {
        var saved = await Service(partLength: 10).Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, Long, TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue(saved.CurrentMessage);
        _saved.Select(part => part.Name).ShouldBe(
        [
            "DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US/1",
            "DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US/2",
            "DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US/3",
        ]);
        _saved.Select(part => part.Ordinal).ShouldBe([1, 2, 3]);
        _saved.ShouldAllBe(part => part.ThingName == "Orders" && part.ThingId == Thing && part.Language == "en-US" && part.DescriptionType == "DataSet");
        DescriptionParts.Join(_saved.Select(part => (part.Ordinal, part.Text))).ShouldBe(Long);
        _descriptions.Verify(d => d.InUnit(It.IsAny<Func<ConfigurationWriteScope, Task<IGenericResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SaveOfATextWithinThePartLengthIsOneRowAtOrdinalOne()
    {
        var saved = await Service(partLength: 100).Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, Long, TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue(saved.CurrentMessage);
        var only = _saved.ShouldHaveSingleItem();
        (only.Ordinal, only.Text).ShouldBe((1, Long));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveOfFewerPartsRemovesEveryPartAboveThemAndKeepsTheOnesItReplaced()
    {
        // The description was saved as three parts and is saved again as one: parts 2 and 3 are stale and must go, part 1 is replaced in place and must not.
        var one = Held("DataSet", Thing, "en-US", 1);
        var two = Held("DataSet", Thing, "en-US", 2);
        var three = Held("DataSet", Thing, "en-US", 3);
        var other = Held("DataSet", Thing, "fr-FR", 2);
        var another = Held("DataSet", Guid.NewGuid(), "en-US", 2);
        _held = [one, two, three, other, another];

        var saved = await Service(partLength: 100).Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, "Short now", TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue(saved.CurrentMessage);
        _saved.ShouldHaveSingleItem().Ordinal.ShouldBe(1);
        _deleted.Order().ShouldBe(new[] { two.Id, three.Id }.Order());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveOfAsManyPartsAsBeforeRemovesNothing()
    {
        _held = [Held("DataSet", Thing, "en-US", 1), Held("DataSet", Thing, "en-US", 2), Held("DataSet", Thing, "en-US", 3)];

        var saved = await Service(partLength: 10).Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, Long, TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue(saved.CurrentMessage);
        _saved.Count.ShouldBe(3);
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveFromOnePartToOnePartIssuesNoDeleteAndDoesNotFailAndSoDoesASaveOfANewDescription()
    {
        _held = [Held("DataSet", Thing, "en-US", 1)];
        var key = new DescriptionKey("DataSet", "Text", Thing, "en-US");

        var again = await Service(partLength: 100).Save(key, "Orders", null, "Orders placed", TestContext.Current.CancellationToken);

        again.IsSuccess.ShouldBeTrue(again.CurrentMessage);
        _deleted.ShouldBeEmpty();

        // Nothing held at all: the removal above the last part finds nothing, which is its normal case, and the save still succeeds.
        _held = [];
        var created = await Service(partLength: 100).Save(key, "Orders", null, "Orders placed", TestContext.Current.CancellationToken);

        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APartThatFailsToSaveStopsTheSaveAndNothingAboveIsRemoved()
    {
        _held = [Held("DataSet", Thing, "en-US", 4)];
        var service = Service(partLength: 10);
        _descriptions
            .Setup(d => d.Save<IDescriptionImplementationConfiguration>(
                It.Is<IDescriptionImplementationConfiguration>(part => part.Ordinal == 2), Domain, Domain, It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(DescriptionsLog.PartLengthInvalid(NullLogger.Instance, 0)));

        var saved = await service.Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, Long, TestContext.Current.CancellationToken);

        saved.IsFailure.ShouldBeTrue();
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ReadManyOfPartsWithAGapIsRefusedAndNeverAnswersTheTextWithAPartMissing()
    {
        TheRowsAre([PartRow(1, "alpha beta"), PartRow(3, " delta")]);

        var read = await ((IDescriptionReader)Service()).ReadMany("DataSet", DescriptionTextSubType.OptionName, [Thing], TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.Code!.Name.ShouldBe("DescriptionPartsBroken");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ReadManyOfAPageOfDescriptionsWithManyPartsIsOneQueryPerChunkOfKeysAndEachIsJoined()
    {
        var a = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var b = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var c = Guid.Parse("00000000-0000-0000-0000-000000000103");
        // Two keys to a chunk: three things are two queries however many parts they have. The rows of a chunk come back together, every part of every description asked for.
        var asked = TheRowsAre(
            [Row("DataSet", a, "A", null, "", "a one "), Part(a, 2, "a two"), Row("DataSet", b, "B", null, "", "b only")],
            [Part(c, 1, "c one "), Part(c, 2, "c two "), Part(c, 3, "c three")]);

        var read = await ((IDescriptionReader)Service()).ReadMany("DataSet", DescriptionTextSubType.OptionName, [a, b, c], TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value![a].Text.ShouldBe("a one a two");
        read.Value[b].Text.ShouldBe("b only");
        read.Value[c].Text.ShouldBe("c one c two c three");
        asked.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task FillOfAPageOfThingsWithLongDescriptionsIsOneQueryAndEachThingGetsTheTextJoined()
    {
        var other = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        var asked = TheRowsAre([Row("Tag", Thing, "urgent", null, "", "Needs "), Part(Thing, 2, "an answer "), Part(Thing, 3, "today"), Row("Tag", other, "late", null, "", "Too late")]);
        var things = new List<FilledThing> { new(Thing), new(other) };

        var filled = await ((IDescriptionReader)Service()).Fill(
            "Tag", DescriptionTextSubType.OptionName, things, thing => thing.Id, (thing, text) => thing.Text = text, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        things[0].Text.ShouldBe("Needs an answer today");
        things[1].Text.ShouldBe("Too late");
        asked.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task FindNamesAThingOnceWhateverNumberOfItsPartsContainTheText()
    {
        var other = Guid.Parse("00000000-0000-0000-0000-0000000000c3");
        var asked = TheRowsAre([Row("DataSetField", Thing, "Total", null, "", "the total one"), Part(Thing, 2, "and the total two"), Row("DataSetField", other, "Net", null, "", "the net total")]);

        var found = await ((IDescriptionReader)Service()).Find("DataSetField", DescriptionTextSubType.OptionName, "total", TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Order().ShouldBe(new[] { other, Thing }.Order());
        asked.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task VersionsAreVersionsOfTheDescriptionAndNotOfEachPart()
    {
        // The reader's rows come from the gateway, which a unit test cannot stand up; the join of a description's versions from the versions of its parts is a pure step.
        var t1 = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddDays(1);
        var written = new List<(int Ordinal, string Text, DateTimeOffset At, bool Deleted)>
        {
            (1, "alpha beta", t1, false), (2, " gamma", t1.AddSeconds(1), false),
            // Saved again as one part: part 1 is written anew and part 2, which it no longer has, is removed, which is a version of it that is a delete.
            (1, "short", t2, false), (2, " gamma", t2.AddSeconds(1), true),
        };

        var versions = DescriptionParts.Versions(written);

        versions.Select(version => version.Text).ShouldBe(["alpha beta gamma", "short"]);
        versions.Select(version => version.WrittenAt).ShouldBe([t1, t2]);
    }

    private static DescriptionDomainConfiguration PartRow(int ordinal, string text) => Part(Thing, ordinal, text);

    private static DescriptionDomainConfiguration Part(Guid thing, int ordinal, string text)
    {
        var row = Row("DataSet", thing, "Orders", null, string.Empty, text);
        row.Ordinal = ordinal;
        return row;
    }
}
