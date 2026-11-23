using System.Collections.Generic;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Query;

/// <summary>
/// FDW-6273: a domain query route declares a field either by the column its record's mapper reaches or as filled, read off the record after the provider's
/// read observer filled it (a description kept in the descriptions table, FDW-6208). A filled field may be asked for; a filter or a sort on one is refused,
/// each with its own code, because the list's table does not hold it.
/// </summary>
[Trait("Category", "Api")]
public sealed class QueryableFieldDeclarationsTests
{
    private static readonly QueryableField Name = new("name", "Name", typeof(string));
    private static readonly QueryableField Description = QueryableField.Filled<DescribedThing>("description", "Description", typeof(string), thing => thing.Description);

    private static readonly DescribedThingMapper Mapper = new();

    [Fact]
    [Trait("Priority", "P0")]
    public void AFilledFieldIsDeclaredByItsGetterAndCannotBeFiltered()
    {
        Description.IsFilled.ShouldBeTrue();
        Description.IsFilterable.ShouldBeFalse();
        Name.IsFilled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AFilledFieldThatTheMapperDoesNotReachIsAValidDeclaration()
    {
        QueryableFieldDeclarations.Check("things", nameof(DescribedThing), [Name, Description], Mapper, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AColumnFieldThatTheMapperDoesNotReachIsRefusedAsTheResourcesDefect()
    {
        var checkedFields = QueryableFieldDeclarations.Check(
            "things", nameof(DescribedThing), [Name, new QueryableField("description", "Description", typeof(string))], Mapper, NullLogger.Instance);

        checkedFields.IsFailure.ShouldBeTrue();
        checkedFields.Messages[^1].Code.ShouldBe("RESTENDPOINTS-60004");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AFilterThatNamesAFilledFieldAnywhereInItsTreeIsRefusedWithItsOwnCode()
    {
        var request = new QueryRequest
        {
            Filter = new QueryFilterNode
            {
                Logic = "And",
                Nodes = [new QueryFilterNode { Field = "name", Operator = "Equal" }, new QueryFilterNode { Field = "DESCRIPTION", Operator = "Contains" }],
            },
        };

        var refused = QueryableFieldDeclarations.Refuse("things", request, [Name, Description], NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Messages[^1].Code.ShouldBe("RESTENDPOINTS-20002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASortOnAFilledFieldIsRefusedWithItsOwnCode()
    {
        var request = new QueryRequest { Sort = [new QuerySort { Field = "name", Direction = "Ascending" }, new QuerySort { Field = "description", Direction = "Descending" }] };

        var refused = QueryableFieldDeclarations.Refuse("things", request, [Name, Description], NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Messages[^1].Code.ShouldBe("RESTENDPOINTS-20003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AFilterAndASortOnColumnsAreLeftToTheTranslator()
    {
        var request = new QueryRequest
        {
            Filter = new QueryFilterNode { Field = "name", Operator = "Equal" },
            Sort = [new QuerySort { Field = "name", Direction = "Ascending" }],
        };

        QueryableFieldDeclarations.Refuse("things", request, [Name, Description], NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ARowCarriesAColumnFromTheMapperAndAFilledFieldFromItsGetter()
    {
        var row = QueryableFieldDeclarations.Row(new DescribedThing { Name = "Margin", Description = "Revenue less cost, over revenue" }, Mapper, [Name, Description]);

        row.ShouldBe(new Dictionary<string, object?> { ["name"] = "Margin", ["description"] = "Revenue less cost, over revenue" });
    }
}
