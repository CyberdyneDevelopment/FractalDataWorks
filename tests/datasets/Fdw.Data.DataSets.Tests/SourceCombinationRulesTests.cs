using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A Compound or Federated data set with more than one source states how they combine, a join or a union (FDW-6946): a data set that states
/// neither is refused, never read as a join or stacked; a statement its joins contradict is refused; every source of a union maps every field.
/// </summary>
public sealed class SourceCombinationRulesTests
{
    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void SeveralSourcesThatStateNeitherAreRefusedNamingTheDataSetAndItsSourceCount()
    {
        var result = SourceCombinationRules.Validate(Compound(combination: null, joined: true), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20033");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("OrderCustomers");
        result.CurrentMessage.ShouldContain("2 sources");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void NothingIsWorkedOutFromTheJoinsAFederatedSetWithJoinsAndNoStatementIsRefusedToo()
    {
        var result = JoinedDataSetRules.Validate(Federated(combination: null, joined: true), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20033");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void OneSourceNeedsNoStatement()
    {
        var single = Compound(combination: null, joined: false);
        single.Sources = [single.Sources[0]];

        SourceCombinationRules.Validate(single, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void ANameThatIsNoCombinationIsRefusedNamingTheChoices()
    {
        var result = SourceCombinationRules.Validate(Compound(combination: "Both", joined: true), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20034");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Join");
        result.CurrentMessage.ShouldContain("Union");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void ACompositionOfOneSourceIsNoWaySeveralSourcesCombine()
    {
        var result = SourceCombinationRules.Validate(Compound(combination: "Singular", joined: false), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20034");
    }

    [Theory]
    [Trait("Category", "DataSets.FDW6946")]
    [InlineData("Union", true)]
    [InlineData("Join", false)]
    public void AStatementTheJoinsContradictIsRefused(string combination, bool joined)
    {
        var result = SourceCombinationRules.Validate(Compound(combination, joined), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20035");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(combination);
    }

    [Theory]
    [Trait("Category", "DataSets.FDW6946")]
    [InlineData("Join", true)]
    [InlineData("join", true)]
    [InlineData("Union", false)]
    public void AStatementThatFitsPasses(string combination, bool joined)
    {
        SourceCombinationRules.Validate(Compound(combination, joined), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void AUnionSourceThatDoesNotMapAFieldIsRefusedNamingTheFieldAndTheSource()
    {
        var union = Compound("Union", joined: false);
        union.Sources[1] = Source("Archive", [("Status", "status")]);

        var result = SourceCombinationRules.Validate(union, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20036");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Archive");
        result.CurrentMessage.ShouldContain("CustomerName");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void AUnionNeedsNoColumnOfACalculatedField()
    {
        var union = Compound("Union", joined: false);
        union.Fields.Add(new DataSetFieldConfiguration { Name = "Label", CalculationName = "LabelOf" });

        SourceCombinationRules.Validate(union, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void ACompoundUnionPassesTheMappingRulesThoughEverySourceMapsEveryField()
    {
        CompoundMappingRules.Validate(Compound("Union", joined: false), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6946")]
    public void IsUnionReadsTheOptionAndNotTheName()
    {
        SourceCombinationRules.IsUnion(Compound("UNION", joined: false)).ShouldBeTrue();
        SourceCombinationRules.IsUnion(Compound("Join", joined: true)).ShouldBeFalse();
        SourceCombinationRules.IsUnion(Compound(null, joined: false)).ShouldBeFalse();
    }

    private static CompoundDataSetConfiguration Compound(string? combination, bool joined) => new()
    {
        Name = "OrderCustomers",
        Implementation = "Compound",
        Fields = [new DataSetFieldConfiguration { Name = "Status" }, new DataSetFieldConfiguration { Name = "CustomerName" }],
        Sources =
        [
            Source("Orders", [("Status", "status"), ("CustomerName", "customer_name")]),
            Source("Customers", [("Status", "state"), ("CustomerName", "name")]),
        ],
        SourceCombination = combination,
        Joins = joined
            ? [new DataSetJoinConfiguration { LeftSourceName = "Orders", LeftFieldName = "CustomerName", RightSourceName = "Customers", RightFieldName = "CustomerName", JoinType = "Inner" }]
            : [],
    };

    private static FederatedDataSetConfiguration Federated(string? combination, bool joined)
    {
        var compound = Compound(combination, joined);
        return new FederatedDataSetConfiguration
        {
            Name = "OrderCustomers",
            Implementation = "Federated",
            FederationStrategy = "Sequential",
            Fields = compound.Fields,
            Sources = compound.Sources,
            SourceCombination = combination,
            Joins = compound.Joins,
        };
    }

    private static DataSetSourceConfiguration Source(string name, IEnumerable<(string Logical, string Physical)> mappings) => new()
    {
        Name = name,
        SourceName = name,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration
        {
            Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical,
        })],
    };
}
