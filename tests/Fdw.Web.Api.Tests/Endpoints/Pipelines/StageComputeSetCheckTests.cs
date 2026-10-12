using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// FDW-1240: a Compute stage whose Built transformation reads and writes tables is checked where the caller reads it. Every
/// declared table, output and single value is bound exactly once, each source and route is well formed, and at most one
/// output replaces the stage's rows.
/// </summary>
public sealed class StageComputeSetCheckTests
{
    private static readonly Guid BuiltId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static BuiltTransformationConfiguration FeeSet()
    {
        var built = new BuiltTransformationConfiguration { Id = BuiltId, Name = "FeeSet", Implementation = BuiltTransformationConfiguration.ImplementationName };
        built.Inputs.Add(new BuiltTransformationInputConfiguration { Name = "pct", ValueType = "Number", ExecutionOrder = 0 });
        BuiltTransformationSetWriter.Apply(built, new CalculationSetDefinition(
            "FeeSet",
            [new TableInputDefinition("Trades", [new TableColumnDefinition("TradeId", "Text"), new TableColumnDefinition("Notional", "Number")])],
            [],
            [
                new TableOutputDefinition("Fees", new EachRowSource("Trades"), [new CopyColumn("TradeId", "TradeId"), new FormulaColumn("Amount", Multiply(Field("Notional"), Field("pct")))]),
                new TableOutputDefinition("Kept", new EachRowSource("Trades"), [new CopyColumn("TradeId", "TradeId")]),
            ])).IsSuccess.ShouldBeTrue();
        return built;
    }

    private static StageComputeCheck Check()
    {
        var built = FeeSet();
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITransformationImplementationConfiguration>.Success(built));
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITransformationImplementationConfiguration>.Success(built));
        return new StageComputeCheck(provider.Object, NullLogger.Instance);
    }

    private static PipelineCompute Stage() => new()
    {
        Transformation = "FeeSet",
        Tables = [new PipelineComputeTable { Input = "Trades", From = "Stage" }],
        Outputs =
        [
            new PipelineComputeOutput { Output = "Fees", Route = "Named", Name = "FeeLines" },
            new PipelineComputeOutput { Output = "Kept", Route = "Replace" },
        ],
        Scalars = [new PipelineComputeScalar { Input = "pct", Value = "0.01", ValueType = "Number" }],
        OnRowFailure = "Fail",
    };

    private static async Task<List<PipelineProblem>> Run(PipelineCompute compute)
    {
        var problems = new List<PipelineProblem>();
        await Check().Check(["Settle"], "Transforms[0]", compute, new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase), problems, TestContext.Current.CancellationToken);
        return problems;
    }

    [Fact]
    public async Task AStageThatBindsEveryTableOutputAndValueHasNoProblem() =>
        (await Run(Stage())).ShouldBeEmpty();

    [Fact]
    public async Task ATableNobodyBindsIsRefusedByName()
    {
        var stage = Stage();
        stage.Tables = [];

        var problems = await Run(stage);

        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("Trades");
    }

    [Fact]
    public async Task ATableBoundTwiceAndATableThePlatformDoesNotDeclareAreEachRefused()
    {
        var twice = Stage();
        twice.Tables = [.. twice.Tables, new PipelineComputeTable { Input = "Trades", From = "Stage" }];
        var unknown = Stage();
        unknown.Tables = [.. unknown.Tables, new PipelineComputeTable { Input = "Prices", From = "Stage" }];

        (await Run(twice)).ShouldNotBeEmpty();
        (await Run(unknown)).Any(problem => problem.Reason.Contains("Prices", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task ASourceThatNamesTheWrongThingIsRefused()
    {
        var stageWithName = Stage();
        stageWithName.Tables[0].Name = "Extra";
        var dataSetWithoutName = Stage();
        dataSetWithoutName.Tables[0].From = "DataSet";
        var unknownKind = Stage();
        unknownKind.Tables[0].From = "Cloud";

        foreach (var stage in new[] { stageWithName, dataSetWithoutName, unknownKind })
        {
            (await Run(stage)).ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async Task AnOutputNobodyRoutesIsRefusedByName()
    {
        var stage = Stage();
        stage.Outputs = [stage.Outputs[1]];

        var problems = await Run(stage);

        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("Fees");
    }

    [Fact]
    public async Task ARouteForAnOutputThePlatformDoesNotDeclareIsRefused()
    {
        var stage = Stage();
        stage.Outputs = [.. stage.Outputs, new PipelineComputeOutput { Output = "Ghost", Route = "Named", Name = "X" }];

        (await Run(stage)).Any(problem => problem.Reason.Contains("Ghost", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task TwoRoutesThatReplaceAnOutputRoutedTwiceAndAMalformedRouteAreEachRefused()
    {
        var twoReplace = Stage();
        twoReplace.Outputs = [new PipelineComputeOutput { Output = "Fees", Route = "Replace" }, twoReplace.Outputs[1]];
        var routedTwice = Stage();
        routedTwice.Outputs = [.. routedTwice.Outputs, new PipelineComputeOutput { Output = "Fees", Route = "Named", Name = "Again" }];
        var namedWithoutName = Stage();
        namedWithoutName.Outputs[0].Name = null;
        var replaceWithName = Stage();
        replaceWithName.Outputs[1].Name = "Oops";
        var unknownRoute = Stage();
        unknownRoute.Outputs[0].Route = "Elsewhere";

        foreach (var stage in new[] { twoReplace, routedTwice, namedWithoutName, replaceWithName, unknownRoute })
        {
            (await Run(stage)).ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async Task ASingleValueNobodyStatesIsRefusedByName()
    {
        var stage = Stage();
        stage.Scalars = [];

        var problems = await Run(stage);

        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("pct");
    }

    [Fact]
    public async Task AStageThatSaysNothingAboutAFailingRowOrNamesAnActionThatDoesNotExistIsRefused()
    {
        var none = Stage();
        none.OnRowFailure = null;
        var blank = Stage();
        blank.OnRowFailure = "  ";
        var unknown = Stage();
        unknown.OnRowFailure = "Ignore";

        (await Run(none)).Single().Reason.ShouldContain("onRowFailure");
        (await Run(blank)).Single().Reason.ShouldContain("onRowFailure");
        (await Run(unknown)).Single().Reason.ShouldContain("Ignore");
    }

    [Theory]
    [InlineData("Fail")]
    [InlineData("Skip")]
    [InlineData("Reject")]
    public async Task EachOfTheThreeActionsIsAccepted(string action)
    {
        var stage = Stage();
        stage.OnRowFailure = action;

        (await Run(stage)).ShouldBeEmpty();
    }

    [Fact]
    public async Task AFieldInputOnATablesStageIsRefused()
    {
        var stage = Stage();
        stage.Inputs = [new PipelineComputeInput { InputName = "pct", SourceField = "x" }];

        (await Run(stage)).ShouldNotBeEmpty();
    }
}
