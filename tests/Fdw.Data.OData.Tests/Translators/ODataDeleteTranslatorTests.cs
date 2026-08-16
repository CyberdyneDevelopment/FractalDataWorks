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

public sealed class ODataDeleteTranslatorTests
{
    private readonly ODataDeleteTranslator _sut = new();

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
        schema.Setup(s => s.GetProjectableFields()).Returns(fields ?? []);

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
        _sut.Name.ShouldBe("ODataDelete");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 42 }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenFilterMissing()
    {
        var container = CreateContainer();
        var command = CreateCommand();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenFilterExpressionIsNull()
    {
        var container = CreateContainer();
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = "not a filter" });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenFilterRootIsNull()
    {
        var container = CreateContainer();
        var filter = new FilterExpression { Root = null };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenIdNotFoundInFilter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteWithIdFieldReturnsDeleteRequest()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 42 }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Method.ShouldBe(HttpMethod.Delete);
        result.Value.RequestUri!.ToString().ShouldBe("/Customers/42");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteWithPrimaryKeyFieldReturnsDeleteRequest()
    {
        var fields = new[] { CreateField("CustomerId").Object };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "CustomerId");
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "CustomerId", Operator = new EqualOperator(), Value = 99 }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/99");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteHandlesContainerNameWithLeadingSlash()
    {
        var container = CreateContainer("/api/Customers");
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/api/Customers/1");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteFindsIdInFilterGroup()
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
                    new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 7 }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/7");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenMetadataIsNull()
    {
        var container = CreateContainer();
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns((IReadOnlyDictionary<string, object>?)null!);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }
}
