using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

// FDW-1435: a scripted command is judged before any store is reached, and the family names what it holds.
public sealed class ScriptCommandTests
{
    private static ScriptParameter[] None => [];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheFamilyNamesItsCommandsAndItsCategory()
    {
        new ScriptReadCommand("SELECT 1", None, null).CommandType.ShouldBe("Read");
        new ScriptRunCommand("DELETE FROM T", None).CommandType.ShouldBe("Run");
        new ScriptRunCommand("DELETE FROM T", None).Category.ShouldBe("Scripted");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASoundScriptHasNoProblem()
    {
        var command = new ScriptReadCommand("SELECT * FROM T WHERE Year = @Year", [new ScriptParameter("Year", 2026)], 100);

        command.FirstProblem().ShouldBeNull();
        command.Take.ShouldBe(100);
        command.Parameters.Single().Value.ShouldBe(2026);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("")]
    [InlineData("   ")]
    public void AScriptWithNoTextIsRefused(string text)
    {
        new ScriptRunCommand(text, None).FirstProblem().ShouldNotBeNull().ShouldContain("no text");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("1Year")]
    [InlineData("Year; DROP TABLE T")]
    [InlineData("@Year")]
    [InlineData("")]
    public void AParameterNameThatIsNotAnIdentifierIsRefused(string name)
    {
        var command = new ScriptRunCommand("SELECT 1", [new ScriptParameter(name, 1)]);

        command.FirstProblem().ShouldNotBeNull().ShouldContain("not a parameter name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterGivenTwiceIsRefusedWhateverItsCase()
    {
        var command = new ScriptRunCommand("SELECT 1", [new ScriptParameter("Year", 1), new ScriptParameter("YEAR", 2)]);

        command.FirstProblem().ShouldNotBeNull().ShouldContain("twice");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(-5)]
    public void AReadThatMayReadNoRowsIsRefused(int take)
    {
        new ScriptReadCommand("SELECT 1", None, take).FirstProblem().ShouldNotBeNull().ShouldContain("at least one");
    }
}
