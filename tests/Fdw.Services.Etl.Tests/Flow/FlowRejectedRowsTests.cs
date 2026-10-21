using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for counting the rows that leave through an error output (FDW-1720): how many left and the reasons they gave are recorded, and
/// no row is kept, because a row can hold a private value.
/// </summary>
public sealed class FlowRejectedRowsTests
{
    private readonly FlowExecutor _sut = new();

    private static TransformContext Context() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>());

    private static PipelineTransformConfiguration Validate() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Check",
        OperationType = "Validate",
        OnFail = "Reject",
        Checks = [new PipelineTransformCheckConfiguration { Name = "Big", Expression = "Id > 3", Ordinal = 1 }],
    };

    private static FlowStageAdapter Check() =>
        new("check", new ValidateTransformType(), Validate(), Context(), batchSize: 4, errorOutput: "bad");

    private static IDictionary<string, object?> Rejected(int id, string? reason)
    {
        var row = Row(id);
        if (reason is not null)
        {
            row[FlowRowMapper.ErrorReasonField] = reason;
        }

        return row;
    }

    private async Task<FlowRunReport> Run(bool linkTheErrorOutput)
    {
        var (good, _) = Sink("good");
        var (bad, _) = Sink("bad");
        var components = new List<IFlowComponent> { Source("src", 10), Check(), good };
        var links = new List<FlowLink> { Link("src", "check"), Link("check", "good") };
        FlowDroppedOutput[] dropped = [];
        if (linkTheErrorOutput)
        {
            components.Add(bad);
            links.Add(Link("check", "bad", output: "bad"));
        }
        else
        {
            dropped = [new FlowDroppedOutput("check", "bad")];
        }

        var result = await _sut.Run(Graph(components, links, dropped), new FlowRunOptions { LinkCapacity = 2 }, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRejectedRowsAreCountedWithTheComponentTheOutputAndTheReason()
    {
        var report = await Run(linkTheErrorOutput: true);

        var rejection = report.Rejections.ShouldNotBeNull().ShouldHaveSingleItem();
        rejection.Component.ShouldBe("check");
        rejection.Output.ShouldBe("bad");
        rejection.Count.ShouldBe(3);
        var reason = rejection.Reasons.ShouldHaveSingleItem();
        reason.Count.ShouldBe(3);
        reason.Reason.ShouldContain("Big");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARejectedRowThatLeavesThroughAnOutputWithNoLinkIsStillCounted()
    {
        var report = await Run(linkTheErrorOutput: false);

        report.Rejections.ShouldNotBeNull().ShouldHaveSingleItem().Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectionHasNoMemberThatCouldHoldARowSoNoPrivateValueReachesTheRecordOfARun()
    {
        typeof(FlowRejection).GetProperty("Rows").ShouldBeNull();
        typeof(FlowRejection).GetProperties().Select(property => property.Name).ShouldBe(["Component", "Output", "Count", "Reasons"], ignoreOrder: true);
        typeof(FlowRejectionReason).GetProperties().Select(property => property.Name).ShouldBe(["Reason", "Count"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheReasonsAreTalliedForEachOutputCommonestFirstAndARowWithNoReasonIsCountedAndInNoReason()
    {
        var sut = new FlowRejectedRows();

        sut.Keep("a", "bad", [Rejected(1, "Big"), Rejected(2, "Odd"), Rejected(3, "Big"), Rejected(4, null)]);
        sut.Keep("a", "other", [Rejected(5, "Odd")]);
        sut.Keep("b", "bad", [Rejected(6, "Big")]);

        var counted = sut.Snapshot();
        counted.Select(c => $"{c.Component}.{c.Output}:{c.Count}").ShouldBe(["a.bad:4", "a.other:1", "b.bad:1"]);
        counted[0].Reasons.Select(r => $"{r.Reason}={r.Count}").ShouldBe(["Big=2", "Odd=1"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PastTheMostReasonsARowIsCountedAndItsReasonIsNotKept()
    {
        var sut = new FlowRejectedRows();
        var rows = Enumerable.Range(1, FlowRejectedRows.MostReasons + 5).Select(i => Rejected(i, $"reason {i}")).ToList();

        sut.Keep("a", "bad", rows);

        var counted = sut.Snapshot().ShouldHaveSingleItem();
        counted.Count.ShouldBe(FlowRejectedRows.MostReasons + 5);
        counted.Reasons.Count.ShouldBe(FlowRejectedRows.MostReasons);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABatchWithNoRowsCountsNothing()
    {
        var sut = new FlowRejectedRows();

        sut.Keep("a", "bad", []);

        sut.Snapshot().ShouldBeEmpty();
    }
}
