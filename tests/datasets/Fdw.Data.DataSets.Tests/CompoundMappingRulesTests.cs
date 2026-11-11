using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A Compound data set's mappings name every column its joined query reads (FDW-5022): a field no source maps, a field two sources map,
/// or a join column its source does not map is refused with its own code, never read from a guessed source.
/// </summary>
public sealed class CompoundMappingRulesTests
{
    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void ASetWhoseMappingsNameEveryColumnPasses()
    {
        CompoundMappingRules.Validate(Compound(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void AStoredFieldNoSourceMapsIsRefusedNamingIt()
    {
        var result = CompoundMappingRules.Validate(Compound(extraField: "Region"), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20018");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Region");
        // DR-9606: the message names the sources the field can be mapped in.
        result.CurrentMessage.ShouldContain("Orders, Customers");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void ACalculatedFieldNeedsNoMapping()
    {
        var dataSet = Compound();
        dataSet.Fields.Add(new DataSetFieldConfiguration { Name = "Label", CalculationName = "Concat" });

        CompoundMappingRules.Validate(dataSet, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void AFieldTwoSourcesMapIsRefusedNamingBoth()
    {
        var result = CompoundMappingRules.Validate(Compound(customersAlsoMap: "Status"), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20019");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Orders");
        result.CurrentMessage.ShouldContain("Customers");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void AJoinColumnItsSourceDoesNotMapIsRefusedNamingIt()
    {
        var result = CompoundMappingRules.Validate(Compound(leftJoinField: "CustomerKey"), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20020");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("CustomerKey");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void AJoinNamingASourceOutsideTheSetIsRefusedNamingIt()
    {
        var dataSet = Compound();
        dataSet.Joins[0].RightSourceName = "Suppliers";

        var result = CompoundMappingRules.Validate(dataSet, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20021");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Suppliers");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public void AFederatedJoinNamingASourceOutsideTheSetIsRefused()
    {
        var compound = Compound();
        var federated = new FederatedDataSetConfiguration
        {
            Name = "Federated",
            Implementation = "Federated",
            Sources = compound.Sources,
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "Ghost", LeftFieldName = "Code", RightSourceName = "Customers", RightFieldName = "Code", JoinType = "Inner" }],
        };

        var result = JoinedDataSetRules.Validate(federated, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20021");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Ghost");
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("None", "DATASETS-20028")]
    [InlineData("Inner; DROP TABLE x", "DATASETS-20029")]
    [InlineData("Outer", "DATASETS-20029")]
    [InlineData("", "DATASETS-20030")]
    [InlineData("   ", "DATASETS-20030")]
    public void ACompoundJoinWhoseTypeDoesNotJoinIsRefusedWithItsOwnCode(string joinType, string code)
    {
        var dataSet = Compound();
        dataSet.Joins[0].JoinType = joinType;

        var result = CompoundMappingRules.Validate(dataSet, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe(code);
        // The message lists the types that join, so the caller can state one.
        var message = result.CurrentMessage.ShouldNotBeNull();
        foreach (var joins in new[] { "Inner", "Left", "Right", "Full", "Cross" })
            message.ShouldContain(joins);
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("Inner")]
    [InlineData("left")]
    [InlineData("RIGHT")]
    [InlineData("Full")]
    public void ACompoundJoinByATypeThatJoinsOnAConditionIsSaved(string joinType)
    {
        var dataSet = Compound();
        dataSet.Joins[0].JoinType = joinType;

        CompoundMappingRules.Validate(dataSet, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5099")]
    public void ACrossJoinReadsNoJoinColumnsSoItNeedsNoneMapped()
    {
        var dataSet = Compound(leftJoinField: "NotMapped");
        dataSet.Joins[0].JoinType = "Cross";

        CompoundMappingRules.Validate(dataSet, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    private static CompoundDataSetConfiguration Compound(string? extraField = null, string? customersAlsoMap = null, string leftJoinField = "CustomerCode")
    {
        var fields = new List<DataSetFieldConfiguration> { new() { Name = "Status" }, new() { Name = "CustomerName" } };
        if (extraField is not null)
            fields.Add(new DataSetFieldConfiguration { Name = extraField });

        var customerMappings = new List<(string, string)> { ("CustomerName", "name"), ("Code", "code") };
        if (customersAlsoMap is not null)
            customerMappings.Add((customersAlsoMap, "status"));

        return new CompoundDataSetConfiguration
        {
            Name = "OrderCustomers",
            Implementation = "Compound",
            Fields = fields,
            Sources =
            [
                Source("Orders", [("Status", "status"), ("CustomerCode", "customer_code")]),
                Source("Customers", customerMappings),
            ],
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "Orders", LeftFieldName = leftJoinField, RightSourceName = "Customers", RightFieldName = "Code", JoinType = "Inner" }],
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
