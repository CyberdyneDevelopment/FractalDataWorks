using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1063: the stand-ins of many fields are read in one pass, and one field takes that same pass. The gateway is a
/// mock that answers by container name, so the tests count how many reads each question costs.
/// </summary>
public sealed class DataSetFieldStandInReaderTests
{
    private const string Store = "ConfigStore";
    private const string Path = "data";
    private const string Parent = "DataSetFieldStandIn";

    private static readonly string[] Containers =
    [
        Parent, "FixedValueStandIn", "SequenceStandIn", "NumericRangeStandIn", "DateRangeStandIn",
        "RegexPatternStandIn", "DrawFromDataSetStandIn", "WeightedPickStandIn", "WeightedPickStandInValue",
    ];

    private sealed class Rig
    {
        public Mock<IDataGateway> Gateway { get; } = new();

        public List<(string Container, IQueryCommand Command)> Calls { get; } = [];

        public Rig()
        {
            Returns<DataSetFieldStandInConfiguration>(Parent);
            Returns<FixedValueStandInConfiguration>("FixedValueStandIn");
            Returns<SequenceStandInConfiguration>("SequenceStandIn");
            Returns<NumericRangeStandInConfiguration>("NumericRangeStandIn");
            Returns<DateRangeStandInConfiguration>("DateRangeStandIn");
            Returns<RegexPatternStandInConfiguration>("RegexPatternStandIn");
            Returns<DrawFromDataSetStandInConfiguration>("DrawFromDataSetStandIn");
            Returns<WeightedPickStandInConfiguration>("WeightedPickStandIn");
            Returns<WeightedPickStandInValueConfiguration>("WeightedPickStandInValue");
        }

        public void Returns<T>(string container, params T[] rows)
            where T : class
            => Gateway
                .Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.Is<DataStoreTarget>(t => t.Container == container), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, target, _) => Calls.Add((target.Container, (IQueryCommand)command)))
                .ReturnsAsync(GenericResult<IEnumerable<T>>.Success(rows));

        public void Fails<T>(string container)
            where T : class
            => Gateway
                .Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.Is<DataStoreTarget>(t => t.Container == container), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IEnumerable<T>>.Failure(new GenericMessage("the store is down")));

        public int Reads(string container) => Calls.Count(c => c.Container == container);

        public Task<IGenericResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>>> Many(params (Guid FieldId, string Name)[] fields)
            => DataSetFieldStandInReader.FindActiveMany(Gateway.Object, Store, Path, fields, NullLogger.Instance, CancellationToken.None);
    }

    private static DataSetFieldStandInConfiguration StandIn(Guid fieldId, Guid id, decimal? nullPercent = null, string? seed = null)
        => new() { Id = id, DataSetFieldId = fieldId, NullPercent = nullPercent, Seed = seed };

    [Fact]
    public async Task ManyFieldsCostOneReadPerTableNotOnePerField()
    {
        var rig = new Rig();
        var fields = Enumerable.Range(0, 9).Select(i => (FieldId: Guid.NewGuid(), Name: "F" + i)).ToArray();
        var standInId = Guid.NewGuid();
        rig.Returns(Parent, StandIn(fields[0].FieldId, standInId));
        rig.Returns("FixedValueStandIn", new FixedValueStandInConfiguration { Id = Guid.NewGuid(), DataSetFieldStandInId = standInId, FixedValue = "x" });

        var result = await rig.Many(fields);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        foreach (var container in Containers.Where(c => c != "WeightedPickStandInValue"))
            rig.Reads(container).ShouldBe(1, container);
        rig.Reads("WeightedPickStandInValue").ShouldBe(0);
    }

    [Fact]
    public async Task AFieldWithoutAStandInIsAbsentNotAnError()
    {
        var rig = new Rig();
        var withOne = Guid.NewGuid();
        var withNone = Guid.NewGuid();
        var standInId = Guid.NewGuid();
        rig.Returns(Parent, StandIn(withOne, standInId, 0.25m, "seed-1"));
        rig.Returns("FixedValueStandIn", new FixedValueStandInConfiguration { Id = Guid.NewGuid(), DataSetFieldStandInId = standInId, FixedValue = "N/A" });

        var result = await rig.Many((withOne, "Conference"), (withNone, "City"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value.ContainsKey(withNone).ShouldBeFalse();
        var found = result.Value[withOne];
        found.Field.ShouldBe("Conference");
        found.Strategy.ShouldBe("FixedValue");
        found.NullPercent.ShouldBe(0.25m);
        found.Seed.ShouldBe("seed-1");
        found.Parameters["fixedValue"].ShouldBe("N/A");
    }

    [Fact]
    public async Task EachStrategyKindIsReadFromItsOwnTable()
    {
        var rig = new Rig();
        var numeric = (Field: Guid.NewGuid(), StandIn: Guid.NewGuid());
        var sequence = (Field: Guid.NewGuid(), StandIn: Guid.NewGuid());
        rig.Returns(Parent, StandIn(numeric.Field, numeric.StandIn), StandIn(sequence.Field, sequence.StandIn));
        rig.Returns("NumericRangeStandIn", new NumericRangeStandInConfiguration { Id = Guid.NewGuid(), DataSetFieldStandInId = numeric.StandIn, MinValue = 1m, MaxValue = 9m, Decimals = 2 });
        rig.Returns("SequenceStandIn", new SequenceStandInConfiguration { Id = Guid.NewGuid(), DataSetFieldStandInId = sequence.StandIn, StartValue = 100, Increment = 5 });

        var result = await rig.Many((numeric.Field, "Amount"), (sequence.Field, "Number"));

        result.Value![numeric.Field].Strategy.ShouldBe("NumericRange");
        result.Value[numeric.Field].Parameters["decimals"].ShouldBe(2);
        result.Value[sequence.Field].Strategy.ShouldBe("Sequence");
        result.Value[sequence.Field].Parameters["increment"].ShouldBe(5L);
    }

    [Fact]
    public async Task WeightedPickValuesComeBackInOrdinalOrderForTheirOwnPick()
    {
        var rig = new Rig();
        var field = Guid.NewGuid();
        var standInId = Guid.NewGuid();
        var pickId = Guid.NewGuid();
        rig.Returns(Parent, StandIn(field, standInId));
        rig.Returns("WeightedPickStandIn", new WeightedPickStandInConfiguration { Id = pickId, DataSetFieldStandInId = standInId });
        rig.Returns("WeightedPickStandInValue",
            new WeightedPickStandInValueConfiguration { Id = Guid.NewGuid(), WeightedPickStandInId = pickId, Value = "B", Weight = 1, Ordinal = 2 },
            new WeightedPickStandInValueConfiguration { Id = Guid.NewGuid(), WeightedPickStandInId = pickId, Value = "A", Weight = 3, Ordinal = 1 },
            new WeightedPickStandInValueConfiguration { Id = Guid.NewGuid(), WeightedPickStandInId = Guid.NewGuid(), Value = "other pick", Weight = 9, Ordinal = 0 });

        var result = await rig.Many((field, "Conference"));

        result.Value![field].Strategy.ShouldBe("WeightedPick");
        var values = ((IEnumerable<object>)result.Value[field].Parameters["values"]!).Select(v => v.ToString()!).ToList();
        values.Count.ShouldBe(2);
        values[0].ShouldContain("Value = A");
        values[1].ShouldContain("Value = B");
    }

    [Fact]
    public async Task OneFieldTakesTheSamePathAsMany()
    {
        var rig = new Rig();
        var field = Guid.NewGuid();
        var standInId = Guid.NewGuid();
        rig.Returns(Parent, StandIn(field, standInId));
        rig.Returns("RegexPatternStandIn", new RegexPatternStandInConfiguration { Id = Guid.NewGuid(), DataSetFieldStandInId = standInId, Pattern = "[A-Z]{3}" });

        var single = await DataSetFieldStandInReader.FindActive(rig.Gateway.Object, Store, Path, field, "Abbreviation", NullLogger.Instance, CancellationToken.None);
        var none = await DataSetFieldStandInReader.FindActive(rig.Gateway.Object, Store, Path, Guid.NewGuid(), "City", NullLogger.Instance, CancellationToken.None);

        single.Value!.Strategy.ShouldBe("RegexPattern");
        single.Value.Parameters["pattern"].ShouldBe("[A-Z]{3}");
        none.IsSuccess.ShouldBeTrue();
        none.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AStandInWithNoStrategyRowFailsLoudly()
    {
        var rig = new Rig();
        var field = Guid.NewGuid();
        rig.Returns(Parent, StandIn(field, Guid.NewGuid()));

        var result = await rig.Many((field, "Conference"));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AFailedReadIsReturnedNotSwallowed()
    {
        var rig = new Rig();
        var field = Guid.NewGuid();
        rig.Returns(Parent, StandIn(field, Guid.NewGuid()));
        rig.Fails<SequenceStandInConfiguration>("SequenceStandIn");

        var result = await rig.Many((field, "Conference"));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task TheFieldIdsAreAnInListAndALongListIsCutIntoPieces()
    {
        var rig = new Rig();
        var fields = Enumerable.Range(0, 501).Select(i => (FieldId: Guid.NewGuid(), Name: "F" + i)).ToArray();

        var result = await rig.Many(fields);

        result.IsSuccess.ShouldBeTrue();
        rig.Reads(Parent).ShouldBe(2);
        var first = (IFilterCondition)((FilterGroup)rig.Calls.First(c => c.Container == Parent).Command.Filter!.Root!).Nodes[0];
        first.PropertyName.ShouldBe("DataSetFieldId");
        first.Operator.Name.ShouldBe("In");
        ((IReadOnlyCollection<object?>)first.Value!).Count.ShouldBe(500);
    }

    [Fact]
    public async Task NoFieldsAskTheStoreNothing()
    {
        var rig = new Rig();

        var result = await rig.Many();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
        rig.Calls.Count.ShouldBe(0);
    }
}
