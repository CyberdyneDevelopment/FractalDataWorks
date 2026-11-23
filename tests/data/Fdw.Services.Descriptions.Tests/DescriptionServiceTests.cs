using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>The descriptions service over stand-ins for the description provider, the settings and the tenant (FDW-6120).</summary>
[Trait("Category", "Descriptions")]
public sealed partial class DescriptionServiceTests
{
    private const string Domain = "Description";
    private static readonly Guid Thing = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid DataSet = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

    // The part length a host that changed nothing has: DescriptionServiceTypes.PartLength.
    private const int PartLength = 4000;

    private readonly Mock<IDescriptionConfigurationProvider> _descriptions = new(MockBehavior.Strict);
    private readonly List<IDescriptionImplementationConfiguration> _saved = [];
    private readonly List<Guid> _deleted = [];
    private List<DescriptionDomainConfiguration> _held = [];

    private DescriptionService Service(string? language = "en-US", int partLength = PartLength)
    {
        // A save with no unit of its own runs in one the provider opens; the unit is not needed to prove what is written in it.
        _descriptions
            .Setup(d => d.InUnit(It.IsAny<Func<ConfigurationWriteScope, Task<IGenericResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<ConfigurationWriteScope, Task<IGenericResult>>, CancellationToken>((work, _) => work(null!));
        SavesInTheUnit();
        // The rows the unit holds now, which a save reads to remove the parts it no longer has and a delete reads to remove what it names.
        _descriptions
            .Setup(d => d.ReadCurrent(It.IsAny<FilterExpression>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FilterExpression filter, ConfigurationWriteScope _, CancellationToken _) =>
                GenericResult<IReadOnlyList<DescriptionDomainConfiguration>>.Success([.. _held.Where(row => Selects(filter, row))]));
        _descriptions
            .Setup(d => d.Delete(It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ConfigurationWriteScope, CancellationToken>((id, _, _) => _deleted.Add(id))
            .ReturnsAsync(GenericResult.Success());
        return new DescriptionService(_descriptions.Object, new StatedLanguage(language), Gateways(), Anonymous(), partLength, NullLogger<DescriptionService>.Instance);
    }

    // What the filter selects of a row, as the table would: the Equal and GreaterThan conditions of the group on the row's own members. The version flags are not members of a row
    // read here, which is current by being held.
    private static bool Selects(FilterExpression filter, DescriptionDomainConfiguration row)
        => ((FilterGroup)filter.Root!).Nodes.OfType<FilterCondition>().Where(condition => condition.PropertyName is not ("IsCurrent" or "IsDeleted")).All(condition =>
        {
            var held = typeof(DescriptionDomainConfiguration).GetProperty(condition.PropertyName)!.GetValue(row)!;
            return condition.Operator.Name switch
            {
                "Equal" => held.Equals(condition.Value),
                "GreaterThan" => Comparer<object>.Default.Compare(held, condition.Value!) > 0,
                _ => throw new NotSupportedException($"The stand-in does not evaluate {condition.Operator.Name}"),
            };
        });

    // Two keys to a chunk, so a read of five things is three queries and a test can say so.
    private static IConfigurationGatewayProvider Gateways()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.SetupGet(g => g.KeyChunkSize).Returns(2);
        return gateways.Object;
    }

    private static IAuthenticationContextAccessor Anonymous()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        return accessor.Object;
    }

    // The one description of the thing in English holds this text as its only part.
    private void Holds(string text) => TheRowsAre([Row("DataSet", Thing, "Orders", null, string.Empty, text)]);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SaveWritesTheTextUnderTheKeyWrittenAsItsName()
    {
        var saved = await Service().Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, "Orders placed", TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue();
        var written = _saved.ShouldHaveSingleItem();
        written.ThingName.ShouldBe("Orders");
        written.OwnerName.ShouldBeNull();
        written.Name.ShouldBe("DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US/1");
        written.Ordinal.ShouldBe(1);
        written.Text.ShouldBe("Orders placed");
        written.DescriptionType.ShouldBe("DataSet");
        written.DescriptionSubType.ShouldBe("Text");
        written.ThingId.ShouldBe(Thing);
        written.Language.ShouldBe("en-US");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SaveKeepsOneRowPerTypeAndPerLanguage()
    {
        var service = Service();

        await service.Save(new DescriptionKey("DataSet", "Text", Thing, "en-US"), "Orders", null, "Orders", TestContext.Current.CancellationToken);
        await service.Save(new DescriptionKey("DataSet", "Text", Thing, "fr-FR"), "Orders", null, "Commandes", TestContext.Current.CancellationToken);
        await service.Save(new DescriptionKey("DataSetField", "Text", Thing, "en-US"), "Total", new DescriptionOwner(DataSet, "Orders"), "The order total", TestContext.Current.CancellationToken);

        _saved.Select(row => row.Name).Distinct().Count().ShouldBe(3);
        _saved.Select(row => row.Ordinal).ShouldAllBe(ordinal => ordinal == 1);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Banana", "Text", "en-US", "Orders", "DescriptionTypeUnknown")]
    [InlineData("", "Text", "en-US", "Orders", "DescriptionTypeUnknown")]
    [InlineData("DataSet", "Banana", "en-US", "Orders", "DescriptionSubTypeUnknown")]
    [InlineData("DataSet", "Text", "", "Orders", "DescriptionLanguageMissing")]
    [InlineData("DataSet", "Text", "en-US", "", "DescriptionTextMissing")]
    [InlineData("DataSet", "Text", "en-US", "   ", "DescriptionTextMissing")]
    public async Task SaveRefusesWhatIsMissingAndWritesNothing(string type, string subType, string language, string text, string code)
    {
        var saved = await Service().Save(new DescriptionKey(type, subType, Thing, language), "Orders", null, text, TestContext.Current.CancellationToken);

        saved.IsFailure.ShouldBeTrue();
        saved.Code!.Name.ShouldBe(code);
        _saved.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SaveRefusesAnEmptyThingId()
    {
        var saved = await Service().Save(new DescriptionKey("DataSet", "Text", Guid.Empty, "en-US"), "Orders", null, "Orders placed", TestContext.Current.CancellationToken);

        saved.IsFailure.ShouldBeTrue();
        saved.Code!.Name.ShouldBe("DescriptionThingMissing");
        _saved.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("DataSet", "", false, "DescriptionThingNameMissing")]
    [InlineData("DataSet", "Orders", true, "DescriptionOwnerNotExpected")]
    [InlineData("DataSetField", "Total", false, "DescriptionOwnerMissing")]
    public async Task SaveRefusesTheNamesAThingOfThatTypeNeedsAndWritesNothing(string type, string thingName, bool hasOwner, string code)
    {
        var owner = hasOwner ? new DescriptionOwner(Guid.NewGuid(), "Sales") : null;

        var saved = await Service().Save(new DescriptionKey(type, "Text", Thing, "en-US"), thingName, owner, "Orders placed", TestContext.Current.CancellationToken);

        saved.IsFailure.ShouldBeTrue();
        saved.Code!.Name.ShouldBe(code);
        _saved.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SaveWritesTheOwnersNameOfAFieldsDescription()
    {
        var saved = await Service().Save(new DescriptionKey("DataSetField", "Text", Thing, "en-US"), "Total", new DescriptionOwner(DataSet, "Orders"), "The order total", TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue();
        var written = _saved.ShouldHaveSingleItem();
        written.ThingName.ShouldBe("Total");
        written.OwnerName.ShouldBe("Orders");
        written.OwnerId.ShouldBe(DataSet);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheWriterRefusesAnUnknownTypeAndAnEmptyThingBeforeItReadsAnything()
    {
        var writer = Service();

        // A refusal comes before the unit is touched, so no unit is needed to prove it.
        var unknown = await writer.DeleteAbout("Banana", Thing, null!, TestContext.Current.CancellationToken);
        var empty = await writer.DeleteOwnedBy("DataSetField", Guid.Empty, null!, TestContext.Current.CancellationToken);

        unknown.Code!.Name.ShouldBe("DescriptionTypeUnknown");
        empty.Code!.Name.ShouldBe("DescriptionThingMissing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeleteRemovesTheDescriptionOfTheKey()
    {
        // Both parts of the description are rows of it, and both go.
        var first = Held("DataSet", Thing, "en-US", 1);
        var second = Held("DataSet", Thing, "en-US", 2);
        _held = [first, second];

        var deleted = await Service().Delete(new DescriptionKey("DataSet", "Text", Thing, "en-US"), TestContext.Current.CancellationToken);

        deleted.IsSuccess.ShouldBeTrue();
        _deleted.Order().ShouldBe(new[] { first.Id, second.Id }.Order());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeleteOfAKeyThatHoldsNothingIsRefusedWithTheCodedMessageOfTheOneFiveLine()
    {
        var key = new DescriptionKey("DataSet", "Text", Thing, "en-US");
        _held = [Held("DataSet", Thing, "fr-FR", 1), Held("DataSet", Guid.NewGuid(), "en-US", 1)];
        // What the provider answered on the 1.5 line, where the description was one row named by its key: a delete of a row that is not there.
        var onTheOneFiveLine = GenericResult.Failure(DefaultConfigurationProviderLog.DeleteTargetNotFound(NullLogger.Instance, nameof(IDescriptionImplementationConfiguration), key.AsName()));

        var deleted = await Service().Delete(key, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        // Why the message's code: a failure built from a MessageLogging message carries no ResultCode object; its code is the message's number.
        deleted.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(onTheOneFiveLine.FirstFailureMessage().ShouldNotBeNull().Message.Code);
        deleted.CurrentMessage.ShouldBe(onTheOneFiveLine.CurrentMessage);
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeWithNoTextOfAThingThatHasNoDescriptionSucceedsAndDeletesNothing()
    {
        var described = await Service().Describe("Tag", DescriptionTextSubType.OptionName, Thing, "urgent", "  ", null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeleteRefusesAnUnknownTypeAndDeletesNothing()
    {
        var deleted = await Service().Delete(new DescriptionKey("Banana", "Text", Thing, "en-US"), TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        _deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LanguageAnswersTheStatedLanguage()
    {
        var language = Service("de-DE").Language();

        language.IsSuccess.ShouldBeTrue();
        language.Value.ShouldBe("de-DE");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LanguageFailsNamingTheSettingWhenNobodyStatedOne()
    {
        var language = Service(language: null).Language();

        language.IsFailure.ShouldBeTrue();
        language.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
    }

    private List<DomainQuery> TheRowsAre(params DescriptionDomainConfiguration[][] pages)
    {
        var asked = new List<DomainQuery>();
        var next = 0;
        _descriptions
            .Setup(d => d.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Callback<DomainQuery, CancellationToken>((query, _) => asked.Add(query))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<DescriptionDomainConfiguration>>.Success(
                new ConfigurationPage<DescriptionDomainConfiguration>(0, int.MaxValue - 1, false, _ => Stream(pages[Math.Min(next++, pages.Length - 1)]))));
        return asked;
    }

    private static DescriptionDomainConfiguration Row(string type, Guid thing, string name, Guid? owner, string ownerName, string text)
        => new()
        {
            DescriptionType = type, DescriptionSubType = DescriptionTextSubType.OptionName, ThingId = thing, Language = "en-US", Ordinal = 1, ThingName = name, OwnerId = owner, OwnerName = ownerName, Text = text,
        };

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ReadManyAnswersTheDescriptionsOfASetInOneQueryPerChunkOfKeysAndNoneForAThingWithout()
    {
        var ids = Enumerable.Range(1, 5).Select(i => Guid.Parse($"00000000-0000-0000-0000-00000000010{i}")).ToArray();
        var asked = TheRowsAre([Row("DataSet", ids[0], "A", null, "", "text of a")], [Row("DataSet", ids[2], "C", null, "", "text of c")], []);

        var read = await ((IDescriptionReader)Service()).ReadMany("DataSet", DescriptionTextSubType.OptionName, ids, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Keys.Order().ShouldBe([ids[0], ids[2]]);
        read.Value[ids[0]].Text.ShouldBe("text of a");
        asked.Count.ShouldBe(3);
        var condition = ((FilterGroup)asked[0].Filter!.Root!).Nodes.OfType<FilterCondition>().Single(c => c.PropertyName == "ThingId");
        ((IEnumerable<object?>)condition.Value!).Count().ShouldBe(2);
        ((FilterGroup)asked[0].Filter!.Root!).Nodes.OfType<FilterCondition>().Single(c => c.PropertyName == "Language").Value.ShouldBe("en-US");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ReadOwnedByFindsTheDescriptionsOfTheThingsOfTheOwnersByTheOwnersId()
    {
        var owner = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
        var asked = TheRowsAre([Row("DataSetField", Thing, "Total", owner, "Orders", "The order total")]);

        var read = await ((IDescriptionReader)Service()).ReadOwnedBy("DataSetField", DescriptionTextSubType.OptionName, [owner], TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value![Thing].Owner!.Id.ShouldBe(owner);
        ((FilterGroup)asked.Single().Filter!.Root!).Nodes.OfType<FilterCondition>().ShouldContain(c => c.PropertyName == "OwnerId");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task FindAnswersTheIdsOfTheThingsWhoseTextContainsTheTextInTheLanguageOfToday()
    {
        var other = Guid.Parse("00000000-0000-0000-0000-0000000000c3");
        var asked = TheRowsAre([Row("DataSetField", Thing, "Total", null, "", "The order total"), Row("DataSetField", other, "Net", null, "", "The net total")]);

        var found = await ((IDescriptionReader)Service()).Find("DataSetField", DescriptionTextSubType.OptionName, "total", TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Order().ShouldBe(new[] { other, Thing }.Order());
        var conditions = ((FilterGroup)asked.Single().Filter!.Root!).Nodes.OfType<FilterCondition>().ToList();
        conditions.Single(c => c.PropertyName == "Text").Value.ShouldBe("total");
        conditions.Single(c => c.PropertyName == "Language").Value.ShouldBe("en-US");
        conditions.Single(c => c.PropertyName == "DescriptionType").Value.ShouldBe("DataSetField");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task FindOfNoTextQueriesNothingAndAnUnknownTypeOrNoLanguageIsRefused()
    {
        var reader = Service();

        var none = await reader.Find("DataSetField", DescriptionTextSubType.OptionName, "  ", TestContext.Current.CancellationToken);
        var unknown = await reader.Find("Banana", DescriptionTextSubType.OptionName, "x", TestContext.Current.CancellationToken);
        var subType = await reader.Find("DataSetField", "Banana", "x", TestContext.Current.CancellationToken);
        var unstated = await ((IDescriptionReader)Service(language: null)).Find("DataSetField", DescriptionTextSubType.OptionName, "x", TestContext.Current.CancellationToken);

        none.IsSuccess.ShouldBeTrue();
        none.Value!.ShouldBeEmpty();
        unknown.IsFailure.ShouldBeTrue();
        subType.IsFailure.ShouldBeTrue();
        unstated.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ReadManyOfNoKeysQueriesNothingAndAnUnknownTypeOrNoLanguageIsRefused()
    {
        var reader = Service();
        var none = await reader.ReadMany("DataSet", DescriptionTextSubType.OptionName, [], TestContext.Current.CancellationToken);
        var unknown = await reader.ReadMany("Banana", DescriptionTextSubType.OptionName, [Thing], TestContext.Current.CancellationToken);
        var subType = await reader.ReadMany("DataSet", "Banana", [Thing], TestContext.Current.CancellationToken);
        var unstated = await ((IDescriptionReader)Service(language: null)).ReadMany("DataSet", DescriptionTextSubType.OptionName, [Thing], TestContext.Current.CancellationToken);

        none.Value!.ShouldBeEmpty();
        unknown.Code!.Name.ShouldBe("DescriptionTypeUnknown");
        subType.Code!.Name.ShouldBe("DescriptionSubTypeUnknown");
        unstated.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        _descriptions.Verify(d => d.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // FDW-6208: Describe and Fill are what the provider of a domain whose things have no owner calls from its write and read observers.
    private void SavesInTheUnit()
        => _descriptions
            .Setup(d => d.Save<IDescriptionImplementationConfiguration>(
                It.IsAny<IDescriptionImplementationConfiguration>(), Domain, Domain, It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<IDescriptionImplementationConfiguration, string, string, string, ConfigurationWriteScope, CancellationToken>((saved, _, _, _, _, _) => _saved.Add(saved))
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeSavesTheTextOfAThingWithNoOwnerInTheLanguageOfTodayInTheUnit()
    {
        var service = Service();
        SavesInTheUnit();

        var described = await service.Describe("Tag", DescriptionTextSubType.OptionName, Thing, "urgent", "Needs an answer today", null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue();
        var written = _saved.ShouldHaveSingleItem();
        written.Name.ShouldBe("Tag/Text/00000000-0000-0000-0000-0000000000a1/en-US/1");
        written.ThingName.ShouldBe("urgent");
        written.OwnerId.ShouldBeNull();
        written.Text.ShouldBe("Needs an answer today");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeFailsWhenNobodyStatedTheLanguageAndWritesNothing()
    {
        var service = Service(language: null);
        SavesInTheUnit();

        var described = await service.Describe("Tag", DescriptionTextSubType.OptionName, Thing, "urgent", "Needs an answer today", null!, TestContext.Current.CancellationToken);

        described.IsFailure.ShouldBeTrue();
        described.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        _saved.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DescribeRefusesAnUnknownTypeAndWritesNothing()
    {
        var service = Service();
        SavesInTheUnit();

        var described = await service.Describe("Banana", DescriptionTextSubType.OptionName, Thing, "urgent", "Needs an answer today", null!, TestContext.Current.CancellationToken);

        described.IsFailure.ShouldBeTrue();
        described.Code!.Name.ShouldBe("DescriptionTypeUnknown");
        _saved.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task FillPutsEachTextOnItsThingWithOneReadAndLeavesAThingWithNoneAsItWas()
    {
        var other = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        var asked = TheRowsAre([Row("Tag", Thing, "urgent", null, "", "Needs an answer today")]);
        var things = new List<FilledThing> { new(Thing), new(other) };

        var filled = await ((IDescriptionReader)Service()).Fill(
            "Tag", DescriptionTextSubType.OptionName, things, thing => thing.Id, (thing, text) => thing.Text = text, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue();
        things[0].Text.ShouldBe("Needs an answer today");
        things[1].Text.ShouldBeNull();
        asked.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task FillFailsWhenNobodyStatedTheLanguageAndFillsNothing()
    {
        var things = new List<FilledThing> { new(Thing) };

        var filled = await ((IDescriptionReader)Service(language: null)).Fill(
            "Tag", DescriptionTextSubType.OptionName, things, thing => thing.Id, (thing, text) => thing.Text = text, TestContext.Current.CancellationToken);

        filled.IsFailure.ShouldBeTrue();
        filled.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        things[0].Text.ShouldBeNull();
    }

    private sealed class FilledThing
    {
        public FilledThing(Guid id) => Id = id;

        public Guid Id { get; }

        public string? Text { get; set; }
    }

    private static async IAsyncEnumerable<DescriptionDomainConfiguration> Stream(IEnumerable<DescriptionDomainConfiguration> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private sealed class StatedLanguage : IEffectiveSettingsProvider
    {
        private readonly string? _language;

        public StatedLanguage(string? language) => _language = language;

        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            value = (T)(object)(_language ?? string.Empty);
            return _language is not null && settingName == SettingDefinitions.DescriptionLanguage;
        }
    }
}
