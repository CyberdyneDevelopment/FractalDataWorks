using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the rows a stage rejects when it runs as a flow component (FDW-1589): they go to the error output with the
/// component and the reason added, and a component with no error output fails the run instead of dropping them.
/// </summary>
public sealed class FlowStageAdapterRejectTests
{
    private readonly FlowExecutor _sut = new();

    private static TransformContext Context() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>());

    private static PipelineTransformConfiguration Validate(string onFail) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Check",
        OperationType = "Validate",
        OnFail = onFail,
        Checks = [new PipelineTransformCheckConfiguration { Name = "Big", Expression = "Id > 3", Ordinal = 1 }],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARejectedRowGoesToTheErrorOutputWithTheComponentAndTheReason()
    {
        var adapter = new FlowStageAdapter("check", new ValidateTransformType(), Validate("Reject"), Context(), batchSize: 4, errorOutput: "bad");
        var (good, goodRows) = Sink("good");
        var (bad, badRows) = Sink("bad");
        var graph = Graph(
            [Source("src", 10), adapter, good, bad],
            [Link("src", "check"), Link("check", "good"), Link("check", "bad", output: "bad")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(goodRows).ShouldBe(new[] { 4, 5, 6, 7, 8, 9, 10 });
        Ids(badRows).ShouldBe(new[] { 1, 2, 3 });
        foreach (var row in badRows)
        {
            row[FlowRowMapper.ErrorComponentField].ShouldBe("check");
            ((string)row[FlowRowMapper.ErrorReasonField]!).ShouldContain("Big");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStageThatRejectsARowWithNoErrorOutputFailsTheRunNamingTheComponent()
    {
        var adapter = new FlowStageAdapter("check", new ValidateTransformType(), Validate("Reject"), Context(), batchSize: 4);
        var (good, _) = Sink("good");
        var graph = Graph([Source("src", 10), adapter, good], [Link("src", "check"), Link("check", "good")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("check");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AValidateThatFailsTheRunStillFailsItWithAnErrorOutput()
    {
        var adapter = new FlowStageAdapter("check", new ValidateTransformType(), Validate("Fail"), Context(), batchSize: 4, errorOutput: "bad");
        var (good, _) = Sink("good");
        var (bad, badRows) = Sink("bad");
        var graph = Graph(
            [Source("src", 10), adapter, good, bad],
            [Link("src", "check"), Link("check", "good"), Link("check", "bad", output: "bad")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        badRows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnErrorOutputCannotBeNamedLikeTheOutput()
    {
        Should.Throw<ArgumentException>(() => new FlowStageAdapter("check", new ValidateTransformType(), Validate("Reject"), Context(), batchSize: 4, errorOutput: "Output"));
    }
}
