using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// The JSON form of a tree round-trips exactly and compiles to the same calculation as the tree it was written from.
/// </summary>
public class CalculationTreeJsonTests
{
    private const string Example =
        "{\"op\":\"When\",\"if\":{\"op\":\"NotEqual\",\"args\":[{\"op\":\"Field\",\"name\":\"q\"},{\"op\":\"Constant\",\"value\":0}]},"
        + "\"then\":{\"op\":\"Divide\",\"args\":[{\"op\":\"Add\",\"args\":[{\"op\":\"Field\",\"name\":\"x\"},{\"op\":\"Field\",\"name\":\"y\"},{\"op\":\"Field\",\"name\":\"w\"}]},{\"op\":\"Field\",\"name\":\"q\"}]},"
        + "\"else\":{\"op\":\"Constant\",\"value\":0}}";

    [Fact]
    public void ATreeRoundTripsExactly()
    {
        var tree = When(Equal(Field("q"), 0), 0, Field("x").Divide(Field("q")).Round(2, "ToEven"));

        var json = CalculationTreeJson.Write(tree);
        var read = CalculationTreeJson.Read(json);

        read.IsSuccess.ShouldBeTrue();
        CalculationTreeJson.Write(read.Value!).ShouldBe(json);
    }

    [Fact]
    public void AWhenNamesItsOperandsIfThenElse()
    {
        var json = CalculationTreeJson.Write(When(Field("f"), 1, 2));

        json.ShouldContain("\"if\":");
        json.ShouldContain("\"then\":");
        json.ShouldContain("\"else\":");
    }

    [Fact]
    public void ARoundCarriesItsPlacesAndMode()
    {
        var json = CalculationTreeJson.Write(Field("x").Round(2, "ToEven"));

        json.ShouldContain("\"places\":2");
        json.ShouldContain("\"mode\":\"ToEven\"");
    }

    [Fact]
    public void AnExactDecimalConstantKeepsEveryDigit()
    {
        var read = CalculationTreeJson.Read(CalculationTreeJson.Write(Constant(0.1234567890123456789m)));

        read.Value!.Value!.Value.ShouldBe(0.1234567890123456789m);
    }

    [Fact]
    public void TheDocumentedExampleReadsAndCompilesToTheGuardedDivision()
    {
        var read = CalculationTreeJson.Read(Example);
        read.IsSuccess.ShouldBeTrue();

        var compiled = new CalculationCompiler().Compile(read.Value!, Limits);
        compiled.IsSuccess.ShouldBeTrue();
        var results = new decimal[1];

        compiled.Value!.Evaluate(Record(("q", 4m), ("x", 1m), ("y", 1m), ("w", 2m)), results).IsSuccess.ShouldBeTrue();
        results[0].ShouldBe(1m);
        compiled.Value!.Evaluate(Record(("q", 0m), ("x", 1m), ("y", 1m), ("w", 2m)), results).IsSuccess.ShouldBeTrue();
        results[0].ShouldBe(0m);
    }

    [Fact]
    public void TextThatIsNotATreeIsRefused()
    {
        RefusalOf(CalculationTreeJson.Read("{")).ShouldBe("TreeJsonInvalid");
        RefusalOf(CalculationTreeJson.Read("[1]")).ShouldBe("TreeJsonInvalid");
        RefusalOf(CalculationTreeJson.Read("{\"args\":[]}")).ShouldBe("TreeJsonInvalid");
        RefusalOf(CalculationTreeJson.Read("{\"op\":\"Nope\"}")).ShouldBe("NodeOperatorUnknown");
        RefusalOf(CalculationTreeJson.Read("{\"op\":\"When\",\"if\":{\"op\":\"Field\",\"name\":\"f\"}}")).ShouldBe("TreeJsonInvalid");
    }
}
