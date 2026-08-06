using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.OData;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.OData.Tests.Translators;

public sealed class ODataQueryTranslatorTests
{
    private readonly ODataQueryTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainer(string name = "Customers")
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    private static Mock<IDataCommand> CreateCommand(Dictionary<string, object>? metadata = null)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(
            metadata != null
                ? new Dictionary<string, object>(metadata, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        return command;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("ODataQuery");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var command = CreateCommand();
        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsGetRequestWithNoParameters()
    {
        var container = CreateContainer();
        var command = CreateCommand();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Method.ShouldBe(HttpMethod.Get);
        result.Value.RequestUri!.ToString().ShouldBe("/Customers");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateAddsFilterQueryParameter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Name",
                Operator = new EqualOperator(),
                Value = "Acme"
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
        uri.ShouldContain("Name");
        uri.ShouldContain("eq");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateAddsSelectQueryParameter()
    {
        var container = CreateContainer();
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = "Name" }, new ProjectionField { PropertyName = "Email" }]
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Projection"] = projection });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$select=");
        uri.ShouldContain("Name");
        uri.ShouldContain("Email");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateAddsOrderByQueryParameter()
    {
        var container = CreateContainer();
        var ordering = new OrderingExpression
        {
            OrderedFields = [new OrderedField { PropertyName = "Name", Direction = SortDirections.ByName("Ascending") }]
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Ordering"] = ordering });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$orderby=");
        uri.ShouldContain("Name");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateAddsPagingParameters()
    {
        var container = CreateContainer();
        var paging = new PagingExpression { Skip = 10, Take = 25 };
        var command = CreateCommand(new Dictionary<string, object> { ["Paging"] = paging });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$skip=10");
        uri.ShouldContain("$top=25");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateHandlesContainerNameWithLeadingSlash()
    {
        var container = CreateContainer("/api/Customers");
        var command = CreateCommand();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldStartWith("/api/Customers");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithCompoundFilter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" },
                    new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateCombinesAllParameters()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new GreaterThanOperator(), Value = 0 }
        };
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = "Name" }]
        };
        var ordering = new OrderingExpression
        {
            OrderedFields = [new OrderedField { PropertyName = "Name", Direction = SortDirections.ByName("Ascending") }]
        };
        var paging = new PagingExpression { Skip = 0, Take = 10 };

        var metadata = new Dictionary<string, object>
        {
            ["Filter"] = filter,
            ["Projection"] = projection,
            ["Ordering"] = ordering,
            ["Paging"] = paging
        };
        var command = CreateCommand(metadata);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
        uri.ShouldContain("$select=");
        uri.ShouldContain("$orderby=");
        uri.ShouldContain("$top=10");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSkipIsOmittedWhenZero()
    {
        var container = CreateContainer();
        var paging = new PagingExpression { Skip = 0, Take = 10 };
        var command = CreateCommand(new Dictionary<string, object> { ["Paging"] = paging });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldNotContain("$skip=");
        uri.ShouldContain("$top=10");
    }
}
