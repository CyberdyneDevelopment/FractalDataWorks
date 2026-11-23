using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>
/// <see cref="DescriptionService.DescribeOwned"/> (FDW-6589): an owner's things of one type described in the unit of the owner's save, with one read of what the owner
/// holds and a write only for what changed. A data store with thousands of fields must not rewrite them all on a save that changed none.
/// </summary>
public sealed partial class DescriptionServiceTests
{
    private static readonly Guid Store = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid FieldA = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid FieldB = Guid.Parse("00000000-0000-0000-0000-0000000000e2");
    private static readonly Guid FieldC = Guid.Parse("00000000-0000-0000-0000-0000000000e3");

    private static readonly DescriptionOwner StoreOwner = new(Store, "NflDb");

    // A part the owner's store holds now: its own row Id, so a removal can be told apart from another.
    private static DescriptionDomainConfiguration Held(Guid thing, string thingName, string text, string ownerName = "NflDb", int ordinal = 1)
    {
        var row = Row("DataContainerField", thing, thingName, Store, ownerName, text);
        row.Id = Guid.NewGuid();
        row.Ordinal = ordinal;
        return row;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeOwnedWritesOnlyTheTextsThatAreNewOrChangedAndLeavesTheSameOnesAlone()
    {
        var service = Service();
        _held = [Held(FieldA, "PlayerId", "The player's key"), Held(FieldB, "Team", "An old text")];

        var described = await service.DescribeOwned(
            "DataContainerField", DescriptionTextSubType.OptionName, StoreOwner,
            [new OwnedDescription(FieldA, "PlayerId", "The player's key"), new OwnedDescription(FieldB, "Team", "The team abbreviation"), new OwnedDescription(FieldC, "Season", "The season year")],
            null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _saved.Select(row => (row.ThingId, row.Text, row.OwnerId, row.OwnerName)).ShouldBe(
            [(FieldB, "The team abbreviation", (Guid?)Store, "NflDb"), (FieldC, "The season year", (Guid?)Store, "NflDb")]);
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeOwnedRemovesTheDescriptionsOfThingsTheOwnerNoLongerHoldsAndOfThingsWhoseTextIsNowEmpty()
    {
        var service = Service();
        var gone = Held(FieldC, "Season", "The season year");
        var emptied = Held(FieldB, "Team", "The team abbreviation");
        _held = [Held(FieldA, "PlayerId", "The player's key"), emptied, gone];

        var described = await service.DescribeOwned(
            "DataContainerField", DescriptionTextSubType.OptionName, StoreOwner,
            [new OwnedDescription(FieldA, "PlayerId", "The player's key"), new OwnedDescription(FieldB, "Team", "  ")],
            null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _saved.ShouldBeEmpty();
        _deleted.Order().ShouldBe(new[] { emptied.Id, gone.Id }.Order());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveThatChangedNoDescriptionReadsTheOwnersDescriptionsOnceAndWritesNothing()
    {
        var service = Service();
        _held = [Held(FieldA, "PlayerId", "The player's key "), Held(FieldA, "PlayerId", "in every table", ordinal: 2), Held(FieldB, "Team", "The team abbreviation")];

        var described = await service.DescribeOwned(
            "DataContainerField", DescriptionTextSubType.OptionName, StoreOwner,
            [new OwnedDescription(FieldA, "PlayerId", "The player's key in every table"), new OwnedDescription(FieldB, "Team", "The team abbreviation")],
            null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _saved.ShouldBeEmpty();
        _deleted.ShouldBeEmpty();
        _descriptions.Verify(d => d.ReadCurrent(It.IsAny<FilterExpression>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AThingRenamedOrAnOwnerRenamedIsWrittenAgainSoTheNamesTheTableHoldsStayTrue()
    {
        var service = Service();
        _held = [Held(FieldA, "PlayerId", "The player's key"), Held(FieldB, "Team", "The team abbreviation", ownerName: "OldStoreName")];

        var described = await service.DescribeOwned(
            "DataContainerField", DescriptionTextSubType.OptionName, StoreOwner,
            [new OwnedDescription(FieldA, "PlayerKey", "The player's key"), new OwnedDescription(FieldB, "Team", "The team abbreviation")],
            null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _saved.Select(row => (row.ThingId, row.ThingName, row.OwnerName)).ShouldBe([(FieldA, "PlayerKey", "NflDb"), (FieldB, "Team", "NflDb")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeOwnedWithNoLanguageStatedFailsBeforeAnythingIsReadOrWritten()
    {
        var service = Service(language: null);

        var described = await service.DescribeOwned(
            "DataContainerField", DescriptionTextSubType.OptionName, StoreOwner, [new OwnedDescription(FieldA, "PlayerId", "The player's key")], null!, TestContext.Current.CancellationToken);

        described.IsFailure.ShouldBeTrue();
        _saved.ShouldBeEmpty();
        _descriptions.Verify(d => d.ReadCurrent(It.IsAny<FilterExpression>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
