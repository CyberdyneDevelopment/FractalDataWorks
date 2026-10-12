using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Executions;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlExecuteTranslatorTests
{
    private readonly MsSqlExecuteTranslator _sut = new();

    private static ExecuteCommand Command(params ExecuteParameter[] parameters) => new(new ExecuteOptions(parameters));

    [Fact]
    public async Task ExecutesTheContainerItIsDispatchedWith()
    {
        var container = TestContainers.Create("etl", "RefreshTotals", "StoredProcedure", TestContainers.Columns("@BatchId"));

        var result = await _sut.Translate(
            Command(new ExecuteParameter("BatchId", new ConstantValueSource(5))), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("EXEC [etl].[RefreshTotals] @BatchId = @fdw_const1");
        result.Value.Parameters["@fdw_const1"].Value.ShouldBe(5);
    }

    [Fact]
    public async Task CurrentTimestampIsDeclaredFirstBecauseAnExecArgumentCannotBeAnExpression()
    {
        var container = TestContainers.Create("etl", "Stamp", "StoredProcedure", TestContainers.Columns("At"));

        var result = await _sut.Translate(
            Command(new ExecuteParameter("At", new CurrentTimestampValueSource())), container.Object, TestContext.Current.CancellationToken);

        result.Value!.CommandText.ShouldBe("DECLARE @fdw_arg0 datetimeoffset(7) = SYSDATETIMEOFFSET(); EXEC [etl].[Stamp] @At = @fdw_arg0");
    }

    [Fact]
    public async Task NoParametersIsAPlainExec()
    {
        var container = TestContainers.Create("etl", "Nightly", "StoredProcedure");

        var result = await _sut.Translate(Command(), container.Object, TestContext.Current.CancellationToken);

        result.Value!.CommandText.ShouldBe("EXEC [etl].[Nightly]");
    }

    [Fact]
    public async Task ATableIsNotExecutable()
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns("Code"));

        var result = await _sut.Translate(Command(), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidExecuteOptions");
    }

    [Fact]
    public async Task ParameterThatIsNotAFieldOfTheContainerIsRefused()
    {
        var container = TestContainers.Create("etl", "Nightly", "StoredProcedure", TestContainers.Columns("Known"));

        var result = await _sut.Translate(
            Command(new ExecuteParameter("Other", new ConstantValueSource(1))), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task SequenceSourceIsRefusedForAParameter()
    {
        var container = TestContainers.Create("etl", "Nightly", "StoredProcedure", TestContainers.Columns("N"));

        var result = await _sut.Translate(
            Command(new ExecuteParameter("N", new NextSequenceValueSource("Seq", ["N"]))), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
