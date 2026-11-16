using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.DataOptionRows;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.DataOptionRows;

/// <summary>
/// FDW-5664: the option rows of a container live in the one options table the server settings name. Every read and write is
/// filtered by the request's data store and container and goes through the gateway set-based. An add and a remove are all or
/// nothing, a change sets only what it states, and each refusal answers its own code before anything is written.
/// </summary>
public sealed class DataOptionRowsTests
{
    private const string Store = "Places";
    private const string Path = "dbo";
    private const string Container = "Visits";
    private static readonly DataStoreTarget OptionsTable = new("ConfigStore", "dcfg", "DataOption");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task EveryOperationRefusesWhileTheOptionsTableIsNotNamed()
    {
        var fixture = new Fixture(settings: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SettingDefinitions.DataOptionsDataStore] = "ConfigStore",
            [SettingDefinitions.DataOptionsContainer] = "DataOption",
        });
        var ct = TestContext.Current.CancellationToken;

        Refused(await fixture.Service.List(new DataOptionRowsQuery { Target = Target(), Skip = 0, Take = 10 }, ct), "DATA-121670");
        Refused(await fixture.Service.Add(Addition(Row("City", "Bern")), ct), "DATA-121670");
        Refused(await fixture.Service.Change(new DataOptionRowChange { Key = Key(Guid.CreateVersion7()), Active = false }, ct), "DATA-121670");
        Refused(await fixture.Service.Remove(new DataOptionRowsRemoval { Target = Target(), Ids = [Guid.CreateVersion7()] }, ct), "DATA-121670");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AListIsOnePageOfTheContainersRowsInColumnThenValueOrder()
    {
        var fixture = new Fixture(reads: [[Stored("City", "Bern"), Stored("City", "Bergen"), Stored("City", "Berlin")]]);

        var page = await fixture.Service.List(new DataOptionRowsQuery { Target = Target(), Column = "City", Skip = 4, Take = 2 }, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        page.Value!.Items.Count.ShouldBe(2);
        page.Value.HasMore.ShouldBeTrue();
        fixture.Targets.Single().ShouldBe(OptionsTable);
        var query = fixture.Sent.Single().ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>();
        query.Paging!.Skip.ShouldBe(4);
        query.Paging.Take.ShouldBe(3);
        query.Ordering!.OrderedFields.Select(f => f.PropertyName).ShouldBe(["Column", "Value"]);
        Equalities(query.Filter).ShouldBe([("DataStore", Store), ("Container", Container), ("Column", "City")]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnAddWritesEveryRowInOneInsertEachWithItsOwnId()
    {
        var fixture = new Fixture(reads: [[]]);

        var added = await fixture.Service.Add(Addition(Row("City", "Bern"), Row("city", "Bergen", option: "Exclude")), TestContext.Current.CancellationToken);

        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        var insert = fixture.Sent.OfType<InsertCommand<Dictionary<string, object?>>>().ShouldHaveSingleItem();
        var rows = insert.Data.ToList();
        rows.Count.ShouldBe(2);
        rows.Select(r => (Guid)r["Id"]!).Distinct().Count().ShouldBe(2);
        rows.ShouldAllBe(r => (string)r["DataStore"]! == Store && (string)r["Container"]! == Container && (string)r["OptionType"]! == "SelectList");
        rows.Select(r => (string)r["Column"]!).ShouldBe(["City", "City"]);
        rows.Select(r => (string)r["Option"]!).ShouldBe(["Include", "Exclude"]);
        added.Value!.Select(r => r.Id).ShouldBe(rows.Select(r => (Guid)r["Id"]!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnOptionTypeDataOptionsDoesNotHaveRefusesTheWholeAddNamingTheRow()
    {
        var fixture = new Fixture(reads: [[]]);

        var added = await fixture.Service.Add(Addition(Row("City", "Bern"), Row("City", "Bergen", optionType: "Ranking")), TestContext.Current.CancellationToken);

        Refused(added, "DATAOPTION-31000");
        added.Messages.Select(m => m.Code).ShouldContain("DATA-21676");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnOptionItsTypeDoesNotHaveIsRefused()
    {
        var fixture = new Fixture(reads: [[]]);

        Refused(await fixture.Service.Add(Addition(Row("City", "Bern", option: "Maybe")), TestContext.Current.CancellationToken), "DATAOPTION-30000");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AColumnTheContainerHasNotDiscoveredIsRefused()
    {
        var fixture = new Fixture(reads: [[]]);

        Refused(await fixture.Service.Add(Addition(Row("Country", "CH")), TestContext.Current.CancellationToken), "DATA-31670");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task AnAddWithNoRowsAnEmptyValueOrNoActiveIsRefused()
    {
        var fixture = new Fixture(reads: [[]]);
        var ct = TestContext.Current.CancellationToken;

        Refused(await fixture.Service.Add(Addition(), ct), "DATA-21670");
        Refused(await fixture.Service.Add(Addition(Row("City", " ")), ct), "DATA-21671");
        Refused(await fixture.Service.Add(Addition(new NewDataOptionRow { Column = "City", Value = "Bern", OptionType = "SelectList", Option = "Include", Active = null }), ct), "DATA-21677");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task APairNamedTwiceInOneAddIsRefused()
    {
        var fixture = new Fixture(reads: [[]]);

        Refused(await fixture.Service.Add(Addition(Row("City", "Bern"), Row("CITY", "Bern", option: "Exclude")), TestContext.Current.CancellationToken), "DATA-21672");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task APairAlreadyStoredRefusesTheAddAndNothingIsWritten()
    {
        var fixture = new Fixture(reads: [[Stored("City", "Bern")]]);

        var added = await fixture.Service.Add(Addition(Row("City", "Bergen"), Row("City", "Bern")), TestContext.Current.CancellationToken);

        Refused(added, "DATA-41670");
        added.Messages.Select(m => m.Code).ShouldContain("DATA-21676");
        fixture.Sent.OfType<InsertCommand<Dictionary<string, object?>>>().ShouldBeEmpty();
        Equalities(fixture.Sent.OfType<QueryCommand<Dictionary<string, object?>>>().Single().Filter).ShouldBe([("DataStore", Store), ("Container", Container)]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AChangeSetsOnlyTheFieldsItStatesOnThatRowOfThatContainer()
    {
        var stored = Stored("City", "Bern");
        var fixture = new Fixture(reads: [[stored]], writes: [1]);

        var changed = await fixture.Service.Change(new DataOptionRowChange { Key = Key((Guid)stored["Id"]!), Option = "Exclude" }, TestContext.Current.CancellationToken);

        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        changed.Value!.Option.ShouldBe("Exclude");
        changed.Value.Value.ShouldBe("Bern");
        var update = fixture.Sent.OfType<UpdateCommand<Dictionary<string, object?>>>().ShouldHaveSingleItem();
        update.Data.Keys.ShouldBe(["Option"]);
        update.Data["Option"].ShouldBe("Exclude");
        Equalities(update.Filter).ShouldBe([("DataStore", Store), ("Container", Container), ("Id", stored["Id"]!)]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AChangeOfARowTheContainerDoesNotHaveIsNotFound()
    {
        var fixture = new Fixture(reads: [[]]);

        Refused(await fixture.Service.Change(new DataOptionRowChange { Key = Key(Guid.CreateVersion7()), Active = false }, TestContext.Current.CancellationToken), "DATA-31671");
        fixture.Sent.OfType<UpdateCommand<Dictionary<string, object?>>>().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task AChangeOfNothingOrToAnEmptyValueIsRefused()
    {
        var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;

        Refused(await fixture.Service.Change(new DataOptionRowChange { Key = Key(Guid.CreateVersion7()) }, ct), "DATA-21673");
        Refused(await fixture.Service.Change(new DataOptionRowChange { Key = Key(Guid.CreateVersion7()), Value = "" }, ct), "DATA-21674");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnIdTheContainerDoesNotHaveRefusesTheRemoveAndNothingIsRemoved()
    {
        var held = Stored("City", "Bern");
        var fixture = new Fixture(reads: [[held]]);

        var removed = await fixture.Service.Remove(new DataOptionRowsRemoval { Target = Target(), Ids = [(Guid)held["Id"]!, Guid.CreateVersion7()] }, TestContext.Current.CancellationToken);

        Refused(removed, "DATA-31672");
        fixture.Sent.OfType<DeleteCommand>().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARemoveDeletesEveryNamedRowInOneCommandOfThatContainer()
    {
        var first = Stored("City", "Bern");
        var second = Stored("City", "Bergen");
        var fixture = new Fixture(reads: [[first, second]], writes: [2]);

        var removed = await fixture.Service.Remove(new DataOptionRowsRemoval { Target = Target(), Ids = [(Guid)first["Id"]!, (Guid)second["Id"]!] }, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        removed.Value.ShouldBe(2);
        var delete = fixture.Sent.OfType<DeleteCommand>().ShouldHaveSingleItem();
        delete.AllRows.ShouldBeFalse();
        Equalities(delete.Filter).ShouldBe([("DataStore", Store), ("Container", Container)]);
        Conditions(delete.Filter).Single(c => c.Operator.Name == "In").PropertyName.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ARemoveWithNoIdsIsRefused()
    {
        var fixture = new Fixture();

        Refused(await fixture.Service.Remove(new DataOptionRowsRemoval { Target = Target(), Ids = [] }, TestContext.Current.CancellationToken), "DATA-21675");
        fixture.Sent.ShouldBeEmpty();
    }

    private static void Refused<T>(IGenericResult<T> result, string code)
    {
        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain(code);
    }

    private static DataOptionRowsContainer Target() => new() { DataStore = Store, Path = Path, Container = Container };

    private static DataOptionRowKey Key(Guid id) => new() { Target = Target(), Id = id };

    private static DataOptionRowsAddition Addition(params NewDataOptionRow[] rows) => new() { Target = Target(), Rows = rows };

    private static NewDataOptionRow Row(string column, string value, string optionType = "SelectList", string option = "Include")
        => new() { Column = column, Value = value, OptionType = optionType, Option = option, Active = true };

    private static Dictionary<string, object?> Stored(string column, string value) => new(StringComparer.Ordinal)
    {
        ["Id"] = Guid.CreateVersion7(),
        ["DataStore"] = Store,
        ["Container"] = Container,
        ["Column"] = column,
        ["Value"] = value,
        ["OptionType"] = "SelectList",
        ["Option"] = "Include",
        ["Active"] = true,
    };

    private static List<(string Field, object Value)> Equalities(IFilterExpression? filter)
        => [.. Conditions(filter).Where(c => c.Operator.Name == "Equal").Select(c => (c.PropertyName, c.Value!))];

    private static IEnumerable<FilterCondition> Conditions(IFilterExpression? filter) => Flatten(filter?.Root);

    private static IEnumerable<FilterCondition> Flatten(IFilterNode? node) => node switch
    {
        FilterCondition condition => [condition],
        FilterGroup group => group.Nodes.SelectMany(Flatten),
        _ => [],
    };

    private sealed class Settings : IEffectiveSettingsProvider
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public Settings(IReadOnlyDictionary<string, string> values)
        {
            _values = values;
        }

        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : throw new InvalidOperationException(settingName);

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            if (_values.TryGetValue(settingName, out var text) && text is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private sealed class Fixture
    {
        private readonly Queue<IReadOnlyList<Dictionary<string, object?>>> _reads;
        private readonly Queue<int> _writes;

        public Fixture(
            IReadOnlyList<IReadOnlyList<Dictionary<string, object?>>>? reads = null,
            IReadOnlyList<int>? writes = null,
            IReadOnlyDictionary<string, string>? settings = null)
        {
            _reads = new Queue<IReadOnlyList<Dictionary<string, object?>>>(reads ?? []);
            _writes = new Queue<int>(writes ?? []);

            var field = new Mock<IField>(MockBehavior.Strict);
            field.Setup(f => f.Name).Returns("City");
            var schema = new Mock<IContainerSchema>(MockBehavior.Strict);
            schema.Setup(s => s.Fields).Returns([field.Object]);
            var container = new Mock<IDataContainer>(MockBehavior.Strict);
            container.Setup(c => c.Schema).Returns(schema.Object);
            var stores = new Mock<IDataStoreProvider>(MockBehavior.Strict);
            stores.Setup(s => s.Get(Store, Path, Container, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

            var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
            gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, target, _) => Record(command, target))
                .ReturnsAsync(() => GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(_reads.Dequeue()));
            gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, target, _) => Record(command, target))
                .ReturnsAsync(() => GenericResult<int>.Success(_writes.Count > 0 ? _writes.Dequeue() : ((InsertCommand<Dictionary<string, object?>>)Sent[^1]).Data.Count()));
            var gateways = new Mock<IDataGatewayProvider>(MockBehavior.Strict);
            gateways.Setup(p => p.ByName(DataGatewayNames.Main)).Returns(gateway.Object);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.Setup(t => t.TenantId).Returns(Guid.CreateVersion7());

            Service = new DataOptionRowService(
                NullLogger<DataOptionRowService>.Instance,
                gateways.Object,
                stores.Object,
                new Settings(settings ?? new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [SettingDefinitions.DataOptionsDataStore] = OptionsTable.DataStore,
                    [SettingDefinitions.DataOptionsPath] = OptionsTable.Path!,
                    [SettingDefinitions.DataOptionsContainer] = OptionsTable.Container,
                }),
                tenant.Object);
        }

        public DataOptionRowService Service { get; }

        public List<IDataCommand> Sent { get; } = [];

        public List<DataStoreTarget> Targets { get; } = [];

        private void Record(IDataCommand command, DataStoreTarget target)
        {
            Sent.Add(command);
            Targets.Add(target);
        }
    }
}
