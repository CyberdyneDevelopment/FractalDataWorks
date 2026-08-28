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

/// <summary>
/// Gap tests for ODataDeleteTranslator - covers: nested group without ID,
/// filter with "ID" (uppercase) field name, null metadata.
/// </summary>
public sealed class ODataDeleteTranslatorGapTests
{
    private readonly ODataDeleteTranslator _sut = new();

    private static Mock<IField> CreateField(string name)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
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
    public async Task TranslateDeleteWithUppercaseIDFieldName()
    {
        // Arrange - "ID" (uppercase) matches case-insensitive check
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "ID", Operator = new EqualOperator(), Value = 99 }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/99");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteWithNestedGroupWithoutIdReturnsFailure()
    {
        // Arrange - filter group with only non-ID fields
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" },
                    new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Test" }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteFindsIdInNestedGroup()
    {
        // Arrange - Id buried in nested group
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterGroup
                    {
                        Operator = LogicalOperator.Or,
                        Nodes =
                        [
                            new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "A" },
                            new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 55 }
                        ]
                    }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/55");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteWithPrimaryKeyFieldNameThatIsNotId()
    {
        // Arrange - PK is "CustomerId", not "Id"
        var fields = new[] { CreateField("CustomerId").Object };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "CustomerId");
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "CustomerId", Operator = new EqualOperator(), Value = 123 }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/123");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteWithGuidResourceId()
    {
        // Arrange
        var container = CreateContainer();
        var guid = Guid.NewGuid();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = guid }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe($"/Customers/{guid}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDeleteWithStringResourceId()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = "abc-123" }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/abc-123");
    }
}
