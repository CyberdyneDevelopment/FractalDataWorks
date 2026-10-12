using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Trees;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// A tree that cannot be evaluated is refused when it is built, with a code that names what is wrong.
/// </summary>
public class CalculationBuildRefusalTests
{
    [Fact]
    public void AForwardReferenceIsRefused()
    {
        var built = CalculationArray.Named("r1").Result("a", Ref("b")).Result("b", Constant(1)).Build(Limits);

        RefusalOf(built).ShouldBe("ForwardReference");
    }

    [Fact]
    public void AResultCannotReferenceItself()
    {
        var built = CalculationArray.Named("r2").Result("a", Ref("a").Add(1)).Build(Limits);

        RefusalOf(built).ShouldBe("ForwardReference");
    }

    [Fact]
    public void AnUnknownOperatorIsRefused()
    {
        var built = CalculationArray.Named("r3").Result("a", CalculationNode.Of("Nope")).Build(Limits);

        RefusalOf(built).ShouldBe("NodeOperatorUnknown");
    }

    [Fact]
    public void AWrongNumberOfOperandsIsRefused()
    {
        var built = CalculationArray.Named("r4").Result("a", CalculationNode.Of("Divide", Field("x"))).Build(Limits);

        RefusalOf(built).ShouldBe("NodeArityInvalid");
    }

    [Fact]
    public void AConditionWhereANumberIsNeededIsRefused()
    {
        var built = CalculationArray.Named("r5").Result("a", Add(Equal(Field("x"), 1), 1)).Build(Limits);

        RefusalOf(built).ShouldBe("OperandTypeMismatch");
    }

    [Fact]
    public void ANumberWhereAConditionIsNeededIsRefused()
    {
        var built = CalculationArray.Named("r6").Result("a", When(Constant(1), 1, 2)).Build(Limits);

        RefusalOf(built).ShouldBe("OperandTypeMismatch");
    }

    [Fact]
    public void AResultThatIsAConditionIsRefused()
    {
        var built = CalculationArray.Named("r7").Result("a", Equal(Field("x"), 1)).Build(Limits);

        RefusalOf(built).ShouldBe("OperandTypeMismatch");
    }

    [Fact]
    public void ADuplicateResultNameIsRefused()
    {
        var built = CalculationArray.Named("r8").Result("a", Constant(1)).Result("a", Constant(2)).Build(Limits);

        RefusalOf(built).ShouldBe("DuplicateResultName");
    }

    [Fact]
    public void AResultThatShadowsAnInputFieldIsRefused()
    {
        var built = CalculationArray.Named("r9").Result("x", Field("x").Add(1)).Build(Limits);

        RefusalOf(built).ShouldBe("ResultShadowsField");
    }

    [Fact]
    public void ARoundNeedsAKnownModeAndPlacesInRange()
    {
        var unknownMode = CalculationArray.Named("r10").Result("a", Field("x").Round(2, "Banker")).Build(Limits);
        var tooManyPlaces = CalculationArray.Named("r11").Result("a", Field("x").Round(29, "ToEven")).Build(Limits);

        RefusalOf(unknownMode).ShouldBe("NodeMemberInvalid");
        RefusalOf(tooManyPlaces).ShouldBe("NodeMemberInvalid");
    }

    [Fact]
    public void AConstantNeedsExactlyOneLiteral()
    {
        var bare = new CalculationNode("Constant", null, null, null, null, null, []);

        RefusalOf(CalculationArray.Named("r12").Result("a", bare).Build(Limits)).ShouldBe("NodeMemberInvalid");
    }

    [Fact]
    public void ALimitThatIsPassedIsRefused()
    {
        var tooManyNodes = CalculationArray.Named("r13").Result("a", Add(Field("x"), Field("y"), Field("w"))).Build(new CalculationLimits(10, 3, 5));
        var tooDeep = CalculationArray.Named("r14").Result("a", Negate(Negate(Negate(Field("x"))))).Build(new CalculationLimits(10, 100, 2));
        var tooManyResults = CalculationArray.Named("r15").Result("a", Constant(1)).Result("b", Constant(2)).Build(new CalculationLimits(1, 100, 5));

        RefusalOf(tooManyNodes).ShouldBe("LimitExceeded");
        RefusalOf(tooDeep).ShouldBe("LimitExceeded");
        RefusalOf(tooManyResults).ShouldBe("LimitExceeded");
    }

    [Fact]
    public void LimitsMustBeStatedAndTheListMustNotBeEmpty()
    {
        var noLimits = CalculationArray.Named("r16").Result("a", Constant(1)).Build(new CalculationLimits(0, 0, 0));
        var noResults = CalculationArray.Named("r17").Build(Limits);

        RefusalOf(noLimits).ShouldBe("ResultListInvalid");
        RefusalOf(noResults).ShouldBe("ResultListInvalid");
    }
}
