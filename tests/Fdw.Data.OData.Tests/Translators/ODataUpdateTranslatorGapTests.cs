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
/// Gap tests for ODataUpdateTranslator - covers: null metadata, null data value,
/// filter with nested group ID, ID property fallback for "ID" casing.
/// </summary>
public sealed class ODataUpdateTranslatorGapTests
{
    private readonly ODataUpdateTranslator _sut = new();

    private sealed class EntityWithID
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class EntityWithoutId
    {
        public string Status { get; set; } = string.Empty;
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
    public async Task TranslateReturnsFailureWhenMetadataIsNull()
    {
        // Arrange
        var container = CreateContainer();
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns((IReadOnlyDictionary<string, object>?)null!);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenDataValueIsNull()
    {
        // Arrange
        var container = CreateContainer();
        var metadata = new Dictionary<string, object>();
        metadata["Data"] = null!;
        var command = CreateCommand(metadata);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateFallsBackToIDPropertyUpperCase()
    {
        // Arrange - entity has "ID" (all caps) property
        var container = CreateContainer();
        var entity = new EntityWithID { ID = 55, Name = "Test" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/55");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateReturnsFailureWhenNoIdPropertyOnEntity()
    {
        // Arrange - no Id, no ID, no PK
        var container = CreateContainer();
        var entity = new EntityWithoutId { Status = "Active" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateExtractsIdFromNestedFilterGroup()
    {
        // Arrange
        var container = CreateContainer();
        var entity = new EntityWithID { ID = 1, Name = "Test" };
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
                            new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 77 }
                        ]
                    }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - should find Id=77 in nested group
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/77");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateUsesFilterIdOverEntityId()
    {
        // Arrange - filter has Id=42, entity has Id=99
        var fields = new[] { CreateField("Id").Object };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "Id");
        var entity = new { Id = 99, Name = "Test" };
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 42 }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - should prefer filter ID over entity property
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/42");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateWithFilterContainingNonIdFieldFallsBackToEntity()
    {
        // Arrange - filter has non-ID field
        var container = CreateContainer();
        var entity = new { Id = 88, Name = "Test" };
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" }
        };
        var command = CreateCommand(new Dictionary<string, object>
        {
            ["Data"] = entity,
            ["Filter"] = filter
        });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - falls back to entity's Id property
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/88");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateFallsBackToPrimaryKeyPropertyInSchema()
    {
        // Arrange - schema has PK named "CustomerId", no Id property in entity
        var fields = new[] { CreateField("CustomerId").Object };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "CustomerId");
        var entity = new { CustomerId = 33, Name = "Test" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers/33");
    }
}
