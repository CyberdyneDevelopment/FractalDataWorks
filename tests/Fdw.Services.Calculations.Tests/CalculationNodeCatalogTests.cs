using System;
using System.Linq;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
using Fdw.Services.Calculations.Abstractions.RelativeTime;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// What each formula operation states about itself for a client that builds a formula editor (FDW-1280): the optional node members
/// it reads and the modes it accepts. The members must match what the operation's own <c>Check</c> reads.
/// </summary>
public sealed class CalculationNodeCatalogTests
{
    [Theory]
    [InlineData("Constant", new[] { "value", "flag" })]
    [InlineData("Field", new[] { "name" })]
    [InlineData("Ref", new[] { "name" })]
    [InlineData("Round", new[] { "places", "mode" })]
    [InlineData("Now", new[] { "name", "mode" })]
    [InlineData("AddTime", new[] { "name", "mode" })]
    public void AnOperationThatReadsExtraMembersNamesThem(string op, string[] members)
    {
        CalculationNodeTypes.ByName(op).Members.ShouldBe(members);
    }

    [Fact]
    public void EveryOtherOperationTakesOnlyOperands()
    {
        var withMembers = new[] { "Constant", "Field", "Ref", "Round", "Now", "AddTime" };

        CalculationNodeTypes.All()
            .Where(type => !withMembers.Contains(type.Name, StringComparer.Ordinal))
            .Where(type => type.Members.Count > 0)
            .Select(type => type.Name)
            .ShouldBeEmpty();
    }

    [Fact]
    public void RoundOffersEveryMidpointMode()
    {
        CalculationNodeTypes.ByName("Round").Modes.ShouldBe(Enum.GetNames(typeof(MidpointRounding)));
    }

    [Fact]
    public void NowOffersEveryAnchorAndAddTimeEveryUnit()
    {
        CalculationNodeTypes.ByName("Now").Modes.ShouldBe(TimeAnchors.All().Select(anchor => anchor.Name));
        CalculationNodeTypes.ByName("AddTime").Modes.ShouldBe(new[] { "Seconds", "Minutes", "Hours", "Days", "Weeks", "Months", "Years" }, ignoreOrder: true);
        CalculationNodeTypes.ByName("AddTime").OperandNames.ShouldBe(new[] { "time", "amount" });
    }

    [Fact]
    public void OnlyRoundAndTheTimeOperationsOfferModes()
    {
        var withModes = new[] { "Round", "Now", "AddTime" };

        CalculationNodeTypes.All()
            .Where(type => !withModes.Contains(type.Name, StringComparer.Ordinal))
            .Where(type => type.Modes.Count > 0)
            .Select(type => type.Name)
            .ShouldBeEmpty();
    }

    [Fact]
    public void EveryOperationStatesItsArityAndAWordOfSummary()
    {
        foreach (var type in CalculationNodeTypes.All())
        {
            type.Summary.ShouldNotBeNullOrWhiteSpace(type.Name);
            type.MinOperands.ShouldBeGreaterThanOrEqualTo(0, type.Name);
            // A negative maximum is "no upper bound" (CalculationArrayValidator reads it so), as Add and Concat have.
            (type.MaxOperands < 0 || type.MaxOperands >= type.MinOperands).ShouldBeTrue(type.Name);
        }
    }
}
