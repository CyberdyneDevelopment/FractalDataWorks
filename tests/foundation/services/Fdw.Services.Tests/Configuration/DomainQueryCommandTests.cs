using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// FDW-978: the command that reads a domain's list -- the version predicates always present, the
/// caller's filter narrowing them, an ordering that is total, and a page that reads one row past its take.
/// </summary>
public class DomainQueryCommandTests
{
    private static readonly DomainConfigurationCommand<DomainConfiguration> Commands = new("Users");

    [Fact]
    public void TheVersionPredicatesAreAlwaysThereWhateverTheCallerAsked()
    {
        var command = Read(new DomainQuery(null, null, 0, 10, false), ["Id"]);

        var conditions = Conditions(command.Filter!.Root);
        conditions.ShouldContain(c => c.PropertyName == "IsCurrent" && Equals(c.Value, true));
        conditions.ShouldContain(c => c.PropertyName == "IsDeleted" && Equals(c.Value, false));
    }

    [Fact]
    public void TheCallersFilterNarrowsTheVersionPredicatesAndIsNeverInsteadOfThem()
    {
        var asked = new FilterCondition { PropertyName = "Email", Operator = FilterOperators.ByName("IsNull"), Value = null };
        var command = Read(new DomainQuery(new FilterExpression { Root = asked }, null, 0, 10, false), ["Id"]);

        var root = command.Filter!.Root.ShouldBeOfType<FilterGroup>();
        root.Operator.ShouldBe(LogicalOperator.And);
        root.Nodes.ShouldContain(asked);
        Conditions(command.Filter.Root).Count.ShouldBe(3);
    }

    [Fact]
    public void WithNoOrderingAskedForTheRecordsComeInNameOrderThenKeyOrder()
    {
        var command = Read(new DomainQuery(null, null, 0, 10, false), ["Id", "RowId"]);

        command.Ordering!.OrderedFields.Select(f => f.PropertyName).ShouldBe(["Name", "Id", "RowId"]);
        command.Ordering.OrderedFields.ShouldAllBe(f => f.Direction == SortDirections.ByName("Ascending"));
    }

    [Fact]
    public void TheCallersOrderingComesFirstAndTheKeysCompleteIt()
    {
        var asked = new OrderingExpression
        {
            OrderedFields = [new OrderedField { PropertyName = "CreatedAt", Direction = SortDirections.ByName("Descending") }],
        };
        var command = Read(new DomainQuery(null, asked, 0, 10, false), ["Id"]);

        command.Ordering!.OrderedFields.Select(f => f.PropertyName).ShouldBe(["CreatedAt", "Id"]);
        command.Ordering.OrderedFields[0].Direction.ShouldBe(SortDirections.ByName("Descending"));
    }

    [Fact]
    public void AKeyTheCallerAlreadyOrderedOnIsNotOrderedOnTwice()
    {
        var asked = new OrderingExpression
        {
            OrderedFields = [new OrderedField { PropertyName = "Id", Direction = SortDirections.ByName("Descending") }],
        };
        var command = Read(new DomainQuery(null, asked, 0, 10, false), ["Id"]);

        command.Ordering!.OrderedFields.ShouldHaveSingleItem().Direction.ShouldBe(SortDirections.ByName("Descending"));
    }

    [Fact]
    public void ThePageReadsOneRowPastTheTake()
    {
        var command = Read(new DomainQuery(null, null, 30, 10, false), ["Id"]);

        command.Paging!.Skip.ShouldBe(30);
        command.Paging.Take.ShouldBe(11);
    }

    [Fact]
    public void ACountIsAnAggregateOverTheSameFilterAndNothingElse()
    {
        var asked = new FilterCondition { PropertyName = "Email", Operator = FilterOperators.ByName("IsNull"), Value = null };
        var command = (QueryCommand<Dictionary<string, object?>>)Commands.Count(
            "PlatformConfiguration", "usr", new DomainQuery(new FilterExpression { Root = asked }, null, 30, 10, true));

        command.Aggregation!.Aggregations[ConfigurationCommandBase<DomainConfiguration>.CountColumn].ShouldBe("COUNT(*)");
        command.Aggregation.GroupByFields.ShouldBeEmpty();
        command.Paging.ShouldBeNull();
        command.Ordering.ShouldBeNull();
        Conditions(command.Filter!.Root).Count.ShouldBe(3);
    }

    [Fact]
    public void ANewVersionIsInsertedWithoutRetiringAnythingAndACreateRetiresTheCurrentRow()
    {
        var record = new DomainConfiguration { Id = Guid.NewGuid(), Name = "a", Domain = "Users", Implementation = "Users" };
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal) { ["TenantId"] = Guid.NewGuid() };

        ((IConfigurationSaveCommand)Commands.Insert("s", "p", record, fields)).ReplacesCurrent.ShouldBeFalse();
        ((IConfigurationSaveCommand)Commands.Create("s", "p", record, fields)).ReplacesCurrent.ShouldBeTrue();
    }

    private static QueryCommand<DomainConfiguration> Read(DomainQuery query, IReadOnlyList<string> keys)
        => (QueryCommand<DomainConfiguration>)Commands.Get("PlatformConfiguration", "usr", query, keys);

    private static List<IFilterCondition> Conditions(IFilterNode? node)
    {
        node.ShouldNotBeNull("the read must carry a filter");
        var found = new List<IFilterCondition>();
        void Walk(IFilterNode n)
        {
            if (n is IFilterCondition condition) found.Add(condition);
            else if (n is FilterGroup group) foreach (var child in group.Nodes) Walk(child);
        }

        Walk(node);
        return found;
    }
}
