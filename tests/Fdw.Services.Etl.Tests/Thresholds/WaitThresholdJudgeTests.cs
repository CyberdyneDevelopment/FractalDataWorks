using System.Linq;
using Fdw.Services.Etl.Thresholds;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for judging thresholds on waiting runs (FDW-3161): one alert while a queue stays over its number, a clear when it falls back, a count of one
/// pipeline or of the whole server, comparisons other than at-or-above, a threshold that is off, and a threshold that is no longer stated.
/// </summary>
public sealed class WaitThresholdJudgeTests
{
    private readonly WaitThresholdJudge _judge = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AThresholdFiresWhenTheCountReachesItsNumberAndNotBefore()
    {
        var queueLong = Threshold("Queue getting long", 50);

        _judge.Judge([queueLong], Counts(49)).Fired.ShouldBeEmpty();
        _judge.Judge([queueLong], Counts(50)).Fired.Select(t => t.Name).ShouldBe(["Queue getting long"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AQueueThatStaysOverTheNumberRaisesOneAlertAndNotOneForEachRunThatJoinsIt()
    {
        var queueLong = Threshold("Queue getting long", 50);

        var first = _judge.Judge([queueLong], Counts(50));
        var second = _judge.Judge([queueLong], Counts(51));
        var third = _judge.Judge([queueLong], Counts(70));

        first.Fired.Count.ShouldBe(1);
        second.Fired.ShouldBeEmpty();
        third.Fired.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AThresholdClearsWhenTheCountFallsBackAndFiresAgainWhenItRisesAgain()
    {
        var queueLong = Threshold("Queue getting long", 50);
        _judge.Judge([queueLong], Counts(50));

        var cleared = _judge.Judge([queueLong], Counts(10));
        var again = _judge.Judge([queueLong], Counts(55));

        cleared.Cleared.ShouldBe(["Server/Queue getting long"]);
        cleared.Fired.ShouldBeEmpty();
        again.Fired.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AThresholdOnOnePipelineCountsThatPipelinesWaitingRunsOnly()
    {
        var afc = Threshold("AFC backlog", 20, owner: "Pipeline:AfcPlayersLoad", pipeline: "AfcPlayersLoad");

        _judge.Judge([afc], Counts(500, ("Other", 500), ("AfcPlayersLoad", 19))).Fired.ShouldBeEmpty();
        _judge.Judge([afc], Counts(21, ("AfcPlayersLoad", 20))).Fired.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AThresholdOnAPipelineWithNoWaitingRunsCountsZero()
    {
        var atZero = Threshold("Never empty", 0, pipeline: "Idle", comparison: "GreaterThan");

        _judge.Judge([atZero], Counts(5)).Fired.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("GreaterThan", 10, false)]
    [InlineData("GreaterThan", 11, true)]
    [InlineData("GreaterThanOrEqual", 10, true)]
    [InlineData("Equal", 10, true)]
    [InlineData("Equal", 11, false)]
    [InlineData("LessThan", 10, false)]
    [InlineData("LessThan", 9, true)]
    [InlineData("NotEqual", 10, false)]
    [InlineData("NotEqual", 9, true)]
    public void TheThresholdUsesTheComparisonItStates(string comparison, int count, bool fires)
    {
        var judged = _judge.Judge([Threshold("T", 10, comparison: comparison)], Counts(count));

        (judged.Fired.Count == 1).ShouldBe(fires);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AThresholdThatIsOffNeverFiresAndClearsWhenItIsSwitchedOff()
    {
        var on = Threshold("T", 5);
        var off = Threshold("T", 5, enabled: false);
        _judge.Judge([on], Counts(9));

        var judged = _judge.Judge([off], Counts(9));

        judged.Fired.ShouldBeEmpty();
        judged.Cleared.ShouldBe(["Server/T"]);
        WaitThresholdJudge.Stopping([Threshold("T", 5, stops: true, enabled: false)], Counts(9)).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AThresholdThatIsNoLongerStatedClearsInsteadOfFiringForEver()
    {
        _judge.Judge([Threshold("T", 5, stops: true)], Counts(9));

        var judged = _judge.Judge([], Counts(9));

        judged.Cleared.ShouldBe(["Server/T"]);
        WaitThresholdJudge.Stopping([], Counts(9)).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TwoOwnersMayStateAThresholdOfTheSameNameAndEachFiresOnItsOwn()
    {
        var server = Threshold("Busy", 10);
        var pipeline = Threshold("Busy", 3, owner: "Pipeline:P", pipeline: "P");

        var judged = _judge.Judge([server, pipeline], Counts(4, ("P", 4)));

        judged.Fired.Select(t => t.Owner).ShouldBe(["Pipeline:P"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OnlyAThresholdThatStopsNewRunsAndHoldsForTheCountStopsThem()
    {
        var notifies = Threshold("Long", 5);
        var stops = Threshold("Full", 8, stops: true);

        WaitThresholdJudge.Stopping([notifies, stops], Counts(6)).ShouldBeNull();
        WaitThresholdJudge.Stopping([notifies, stops], Counts(8)).ShouldBeSameAs(stops);
        WaitThresholdJudge.Stopping([notifies], Counts(8)).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStoppingThresholdIsJudgedAgainstTheCountItIsGivenWhetherOrNotAnyChangeWasJudgedBefore()
    {
        var stops = Threshold("Full", 3, stops: true, owner: "Pipeline:P", pipeline: "P");

        WaitThresholdJudge.Stopping([stops], Counts(0, ("P", 3))).ShouldBeSameAs(stops);
        WaitThresholdJudge.Stopping([stops], Counts(9, ("P", 2))).ShouldBeNull();
    }
}
