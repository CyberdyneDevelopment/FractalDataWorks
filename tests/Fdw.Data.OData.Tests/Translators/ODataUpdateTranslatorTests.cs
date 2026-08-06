using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.OData;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.OData.Tests.Translators;

public sealed class ODataUpdateTranslatorTests
{
    private readonly ODataUpdateTranslator _sut = new();

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static Mock<IField> CreateField(string name)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        // Why: IsPrimaryKey removed from IField — PK identity resolved from container Metadata["SurrogateKeyField"].
        return field;
    }

    private static Mock<IStorageContainer> CreateContainer(
        string name = "Customers",
        IField[]? fields = null,
        string? primaryKeyFieldName = null)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields ?? []);

        // Why: GetPrimaryKeyFieldName() reads Metadata["SurrogateKeyField"] — set up here
        // to replace the removed IField.IsPrimaryKey approach.
        var metadata = new Dictionary<string, object>();
        if (primaryKeyFieldName != null)
            metadata["SurrogateKeyField"] = primaryKeyFieldName;

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(metadata);
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
        _sut.Name.ShouldBe("ODataUpdate");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var entity = new TestEntity { Id = 1, Name = "Acme" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenDataMissing()
    {
        var container = CreateContainer();
        var command = CreateCommand();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenResourceIdNotFound()
    {
        var container = CreateContainer();
        var entity = new { Status = "Active" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateWithFilterIdReturnsPutRequest()
    {
        var container = CreateContainer();
        var entity = new TestEntity { Id = 1, Name = "Updated" };
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 42 }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Method.ShouldBe(HttpMethod.Put);
        result.Value.RequestUri!.ToString().ShouldBe("/Customers/42");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateSetsJsonContent()
    {
        var container = CreateContainer();
        var entity = new TestEntity { Id = 1, Name = "Updated" };
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Content.ShouldNotBeNull();
        result.Value.Content!.Headers.ContentType!.MediaType.ShouldBe("application/json");

        var body = await result.Value.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Updated");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateFallsBackToEntityPrimaryKeyProperty()
    {
        var fields = new[] { CreateField("Id").Object };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "Id");
        var entity = new TestEntity { Id = 99, Name = "FallbackTest" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/99");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateFallsBackToIdPropertyByConvention()
    {
        var container = CreateContainer();
        var entity = new TestEntity { Id = 77, Name = "ConventionTest" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/77");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateHandlesContainerNameWithLeadingSlash()
    {
        var container = CreateContainer("/api/Customers");
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/api/Customers/1");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateWithPrimaryKeyFieldInFilter()
    {
        var fields = new[] { CreateField("CustomerId").Object };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "CustomerId");
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "CustomerId", Operator = new EqualOperator(), Value = 55 }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/55");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateFindsIdInFilterGroup()
    {
        var container = CreateContainer();
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" },
                    new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 33 }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/33");
    }
}
