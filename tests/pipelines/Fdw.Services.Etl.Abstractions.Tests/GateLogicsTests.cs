using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>FDW-6134: the seven logics a gate judges its lines by, each over every combination of two lines, and the number of lines each takes.</summary>
[Trait("Category", "GateLogics")]
public sealed class GateLogicsTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("And", false, false, false)]
    [InlineData("And", false, true, false)]
    [InlineData("And", true, false, false)]
    [InlineData("And", true, true, true)]
    [InlineData("Or", false, false, false)]
    [InlineData("Or", false, true, true)]
    [InlineData("Or", true, false, true)]
    [InlineData("Or", true, true, true)]
    [InlineData("Nand", false, false, true)]
    [InlineData("Nand", false, true, true)]
    [InlineData("Nand", true, false, true)]
    [InlineData("Nand", true, true, false)]
    [InlineData("Nor", false, false, true)]
    [InlineData("Nor", false, true, false)]
    [InlineData("Nor", true, false, false)]
    [InlineData("Nor", true, true, false)]
    [InlineData("Xor", false, false, false)]
    [InlineData("Xor", false, true, true)]
    [InlineData("Xor", true, false, true)]
    [InlineData("Xor", true, true, false)]
    [InlineData("Xnor", false, false, true)]
    [InlineData("Xnor", false, true, false)]
    [InlineData("Xnor", true, false, false)]
    [InlineData("Xnor", true, true, true)]
    public void ALogicJudgesEveryCombinationOfTwoLines(string name, bool first, bool second, bool expected) =>
        GateLogics.ByName(name).Judge([first, second]).ShouldBe(expected);

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void NotAnswersTheOppositeOfItsOneLine(bool line, bool expected) =>
        GateLogics.ByName("Not").Judge([line]).ShouldBe(expected);

    [Fact]
    [Trait("Priority", "P0")]
    public void XorOfThreeLinesIsTrueWhenAnOddNumberAreTrue()
    {
        GateLogics.ByName("Xor").Judge([true, true, true]).ShouldBeTrue();
        GateLogics.ByName("Xor").Judge([true, true, false]).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void NotTakesExactlyOneLineAndEveryOtherLogicAtLeastTwo()
    {
        GateLogics.All().Select(logic => logic.Name).Order().ShouldBe(["And", "Nand", "Nor", "Not", "Or", "Xnor", "Xor"]);
        var not = GateLogics.ByName("Not");
        (not.MinimumInputs, not.MaximumInputs).ShouldBe((1, 1));
        foreach (var logic in GateLogics.All().Where(logic => logic.Name != "Not"))
        {
            (logic.MinimumInputs, logic.MaximumInputs).ShouldBe((2, null), logic.Name);
        }
    }
}
