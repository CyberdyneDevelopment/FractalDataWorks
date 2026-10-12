using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Executions;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Commands.Data.Tests.Loads;

public sealed class InsertFromQueryAndExecuteOptionsTests
{
    [Fact]
    public void InsertFromQueryNeedsAtLeastOneTargetColumn() =>
        new InsertFromQueryOptions([], new QuerySource("Staging")).FirstProblem().ShouldNotBeNull();

    [Fact]
    public void ValueSourceColumnsCountAsTargetColumns()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging"))
        {
            ValueSources = [new ColumnValueBinding("Seq", new NextSequenceValueSource("S", ["Code"]))],
        };

        options.FirstProblem().ShouldBeNull();
        options.TargetColumns.ToArray().ShouldBe(new[] { "Code", "Seq" });
    }

    [Fact]
    public void AColumnInBothListsIsRefused()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging"))
        {
            ValueSources = [new ColumnValueBinding("code", new ConstantValueSource("x"))],
        };

        options.FirstProblem().ShouldNotBeNull();
    }

    [Fact]
    public void InsertFromQueryCommandTypeIsTheVerb() =>
        new InsertFromQueryCommand(new InsertFromQueryOptions(["Code"], new QuerySource("Staging"))).CommandType.ShouldBe("InsertFromQuery");

    [Fact]
    public void NextSequenceKindsHaveTheContractNames()
    {
        new NextSequenceValueSource("S", ["Code"]).Kind.ShouldBe("NextSequenceValue");
        new ColumnReferenceValueSource("Code").Kind.ShouldBe("Column");
        new ConstantValueSource(1).Kind.ShouldBe("Constant");
        new CurrentTimestampValueSource().Kind.ShouldBe("CurrentTimestamp");
    }

    [Fact]
    public void ExecuteParametersMustBeNamedOnce()
    {
        var twice = new ExecuteOptions([new ExecuteParameter("A", new ConstantValueSource(1)), new ExecuteParameter("a", new ConstantValueSource(2))]);

        twice.FirstProblem().ShouldNotBeNull();
        new ExecuteOptions([]).FirstProblem().ShouldBeNull();
    }

    [Fact]
    public void ExecuteCommandTypeIsTheVerb() =>
        new ExecuteCommand(new ExecuteOptions([])).CommandType.ShouldBe("Execute");
}
