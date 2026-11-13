using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-5347: a list held in a variable renders as <c>"A","B","C"</c> wherever a variable becomes text: a <c>{{Name}}</c> setting such as a
/// source's path, the text of a double-brace parameter, and a script's <c>$(Name)</c> token.
/// </summary>
public sealed class ListVariableRenderingTests
{
    private static readonly string[] Letters = ["A", "B", "C"];

    private static Dictionary<string, object?> Values(object? value) => new(StringComparer.OrdinalIgnoreCase) { ["Names"] = value };

    [Fact]
    public void AListIsQuotedAndCommaSeparated()
    {
        VariableListText.TryRender(Letters, out var text).ShouldBeTrue();

        text.ShouldBe("\"A\",\"B\",\"C\"");
    }

    [Fact]
    public void AnEmptyListIsRefusedBecauseNoTextStandsForIt()
    {
        VariableListText.TryRender(Array.Empty<string>(), out var text).ShouldBeFalse();

        text.ShouldBeNull();
    }

    [Fact]
    public void AnEmptyListIsRefusedInAScriptSoTheDatabaseIsNeverAskedForAnEmptyIn()
    {
        var rendered = ScriptTokens.Render("run", "SELECT * FROM T WHERE Region IN ($(Names))", Values(Array.Empty<string>()), NullLogger.Instance);

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("Names");
    }

    [Fact]
    public void NumbersAreWrittenInTheInvariantCulture()
    {
        VariableListText.TryRender(new object[] { 1, 2.5m }, out var text).ShouldBeTrue();

        text.ShouldBe("\"1\",\"2.5\"");
    }

    [Theory]
    [InlineData("a\"b")]
    [InlineData(null)]
    public void AnItemThatCannotBeQuotedRefusesTheListAndWritesNothing(string? item)
    {
        VariableListText.TryRender(new[] { "A", item }, out var text).ShouldBeFalse();

        text.ShouldBeNull();
    }

    [Fact]
    public void OnlyWhatAForEachReadsAsAListIsAList()
    {
        VariableListText.IsList(Letters).ShouldBeTrue();
        VariableListText.IsList(new List<object?> { "A" }).ShouldBeTrue();
        VariableListText.IsList("ABC").ShouldBeFalse();
        VariableListText.IsList(5).ShouldBeFalse();
        VariableListText.IsList(null).ShouldBeFalse();
    }

    [Fact]
    public void AListInAParameterTextRendersQuoted()
    {
        var variables = new FlowVariables();
        variables.Declare("Names", Letters.ToList());

        VariableText.Render("Names: {{Names}}", variables).ShouldBe("Names: \"A\",\"B\",\"C\"");
    }

    [Fact]
    public void AParameterThatIsOneListVariableKeepsTheListAsAList()
    {
        var variables = new FlowVariables();
        variables.Declare("Names", Letters.ToList());

        VariableText.Typed("{{Names}}")(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(Letters);
    }

    [Fact]
    public void AListWithAnItemThatCannotBeQuotedThrowsWhereAParameterTextIsRendered()
    {
        var variables = new FlowVariables();
        variables.Declare("Names", new List<string> { "A\"B" });

        Should.Throw<InvalidOperationException>(() => VariableText.Render("{{Names}}", variables)).Message.ShouldContain("Names");
    }

    [Theory]
    [InlineData("A\"B")]
    [InlineData(null)]
    public async Task AnExecuteSqlStepWhoseParameterNamesAListItThatCannotBeQuotedFailsWithTheCodedMessageAndRunsNothing(string? item)
    {
        var fake = new ScriptingGateway([], affected: 1);
        var variables = new FlowVariables();
        variables.Declare("Names", new List<string?> { "A", item });
        var step = new FlowExecuteSql(
            "purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.T WHERE Region = @Region",
            [new FlowSqlParameter("Region", VariableText.Typed("in ({{Names}})"))], null);

        var ran = await step.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "FLOW-21010");
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("purge");
        ran.CurrentMessage.ShouldContain("Names");
        fake.Commands.ShouldBeEmpty();
    }

    [Fact]
    public void TheRenderThrowIsAnInvalidOperationExceptionThatCarriesTheListNameSoExistingCallersAreUnchanged()
    {
        var variables = new FlowVariables();
        variables.Declare("Names", new List<string> { "A\"B" });

        var thrown = Should.Throw<InvalidOperationException>(() => VariableText.Render("{{Names}}", variables));

        thrown.GetType().ShouldBe(typeof(InvalidOperationException));
        VariableListNotWritable.TryRead(thrown, out var listName).ShouldBeTrue();
        listName.ShouldBe("Names");
    }

    [Fact]
    public void AListInASourcePathSettingRendersQuoted()
    {
        var rendered = ComponentParameters.Text("source", "path", "in/{{Names}}", Values(Letters), NullLogger.Instance);

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value.ShouldBe("in/\"A\",\"B\",\"C\"");
    }

    [Fact]
    public void AListWithAnItemThatCannotBeQuotedFailsASettingNamingTheOwnerTheSettingAndTheList()
    {
        var rendered = ComponentParameters.Text("source", "path", "in/{{Names}}", Values(new[] { "A\"B" }), NullLogger.Instance);

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("source");
        rendered.CurrentMessage.ShouldContain("path");
        rendered.CurrentMessage.ShouldContain("Names");
        rendered.CurrentMessage.ShouldNotContain("A\"B", Case.Sensitive);
    }

    [Fact]
    public void AListInAScriptTokenRendersQuoted()
    {
        var rendered = ScriptTokens.Render("run", "SELECT * FROM T WHERE Region IN ($(Names))", Values(Letters), NullLogger.Instance);

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value.ShouldBe("SELECT * FROM T WHERE Region IN (\"A\",\"B\",\"C\")");
    }

    [Theory]
    [InlineData("A\"); DROP TABLE T; --")]
    [InlineData("A\"")]
    public void AListItemThatCouldCloseItsQuotesIsRefusedInAScriptAndIsNotWritten(string item)
    {
        var rendered = ScriptTokens.Render("run", "SELECT * FROM T WHERE Region IN ($(Names))", Values(new[] { "B", item }), NullLogger.Instance);

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("Names");
        rendered.CurrentMessage.ShouldNotContain("DROP", Case.Sensitive);
    }

    [Fact]
    public void AValueThatIsNotAListIsStillHeldToANameInAScript()
    {
        ScriptTokens.Render("run", "SELECT * FROM $(Names)", Values("a b"), NullLogger.Instance).IsFailure.ShouldBeTrue();
        ScriptTokens.Render("run", "SELECT * FROM $(Names)", Values("T"), NullLogger.Instance).Value.ShouldBe("SELECT * FROM T");
    }
}
