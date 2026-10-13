using System;
using System.Linq;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
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
    public void AnOperationThatReadsExtraMembersNamesThem(string op, string[] members)
    {
        CalculationNodeTypes.ByName(op).Members.ShouldBe(members);
    }

    [Fact]
    public void EveryOtherOperationTakesOnlyOperands()
    {
        var withMembers = new[] { "Constant", "Field", "Ref", "Round" };

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
    public void OnlyRoundOffersModes()
    {
        CalculationNodeTypes.All()
            .Where(type => !string.Equals(type.Name, "Round", StringComparison.Ordinal))
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
            type.MaxOperands.ShouldBeGreaterThanOrEqualTo(type.MinOperands, type.Name);
        }
    }
}
