using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.FieldAccess;

namespace Fdw.Commands.Data.Tests;

public sealed class ResultMergerAdditionalTests
{
    private readonly ResultMerger _merger = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullFieldExtractorThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ResultMerger(null!, new QualifiedNameParser()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullNameParserThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ResultMerger(new CompositeFieldExtractor(), null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinThrowsWhenLeftRecordsIsNull()
    {
        var joinDef = CreateJoinDefinition("Inner");
        Should.Throw<ArgumentNullException>(() =>
            _merger.HashJoin<dynamic, dynamic, dynamic>(null!, [], joinDef, (l, r) => l));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinThrowsWhenRightRecordsIsNull()
    {
        var joinDef = CreateJoinDefinition("Inner");
        Should.Throw<ArgumentNullException>(() =>
            _merger.HashJoin<dynamic, dynamic, dynamic>([], null!, joinDef, (l, r) => l));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinThrowsWhenJoinDefinitionIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            _merger.HashJoin<dynamic, dynamic, dynamic>([], [], null!, (l, r) => l));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinThrowsWhenResultSelectorIsNull()
    {
        var joinDef = CreateJoinDefinition("Inner");
        Should.Throw<ArgumentNullException>(() =>
            _merger.HashJoin<dynamic, dynamic, dynamic>([], [], joinDef, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinThrowsWhenNoConditions()
    {
        var joinDef = new JoinDefinition
        {
            ContainerName = "Orders",
            JoinType = JoinTypes.ByName("Inner"),
            Conditions = new List<(string LeftField, string RightField)>()
        };
        Should.Throw<InvalidOperationException>(() =>
            _merger.HashJoin<dynamic, dynamic, dynamic>(
                new List<dynamic>(),
                new List<dynamic>(),
                joinDef,
                (l, r) => l).ToList());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsWhenLeftRecordsIsNull()
    {
        var joinDef = CreateJoinDefinition("Inner");
        Should.Throw<ArgumentNullException>(() =>
            _merger.NestedLoopJoin<dynamic, dynamic, dynamic>(null!, [], joinDef, (l, r) => l));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsWhenRightRecordsIsNull()
    {
        var joinDef = CreateJoinDefinition("Inner");
        Should.Throw<ArgumentNullException>(() =>
            _merger.NestedLoopJoin<dynamic, dynamic, dynamic>([], null!, joinDef, (l, r) => l));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsWhenJoinDefinitionIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            _merger.NestedLoopJoin<dynamic, dynamic, dynamic>([], [], null!, (l, r) => l));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsWhenResultSelectorIsNull()
    {
        var joinDef = CreateJoinDefinition("Inner");
        Should.Throw<ArgumentNullException>(() =>
            _merger.NestedLoopJoin<dynamic, dynamic, dynamic>([], [], joinDef, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsWhenNoConditions()
    {
        var joinDef = new JoinDefinition
        {
            ContainerName = "Orders",
            JoinType = JoinTypes.ByName("Inner"),
            Conditions = new List<(string LeftField, string RightField)>()
        };
        Should.Throw<InvalidOperationException>(() =>
            _merger.NestedLoopJoin<dynamic, dynamic, dynamic>(
                new List<dynamic>(),
                new List<dynamic>(),
                joinDef,
                (l, r) => l).ToList());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsForUnsupportedJoinType()
    {
        var joinDef = CreateJoinDefinition("Right");
        var left = new List<dynamic> { CreateRecord(new { Id = 1 }) };
        var right = new List<dynamic> { CreateRecord(new { CustomerId = 1 }) };

        Should.Throw<NotSupportedException>(() =>
            _merger.NestedLoopJoin(left, right, joinDef, (l, r) => l).ToList());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinThrowsForNotFoundExecutor()
    {
        // Arrange - Use a mock IJoinType with a name that has no executor
        var mockJoinType = new Mock<IJoinType>();
        mockJoinType.Setup(j => j.Name).Returns("NonExistent");

        var mockJoinDef = new Mock<IJoinDefinition>();
        mockJoinDef.Setup(j => j.JoinType).Returns(mockJoinType.Object);
        mockJoinDef.Setup(j => j.Conditions).Returns(
            new List<(string LeftField, string RightField)> { ("Id", "Id") });

        var left = new List<dynamic> { CreateRecord(new { Id = 1 }) };
        var right = new List<dynamic> { CreateRecord(new { Id = 1 }) };

        // Act & Assert
        Should.Throw<NotSupportedException>(() =>
            _merger.HashJoin(left, right, mockJoinDef.Object, (l, r) => l).ToList());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoinThrowsForNotFoundExecutor()
    {
        // Arrange - Use a mock IJoinType with name "Inner" to pass the type check
        // but where ByName returns NotFound (this can happen if no executors are registered for it)
        var mockJoinType = new Mock<IJoinType>();
        mockJoinType.Setup(j => j.Name).Returns("NonExistentInner");

        var mockJoinDef = new Mock<IJoinDefinition>();
        mockJoinDef.Setup(j => j.JoinType).Returns(mockJoinType.Object);
        mockJoinDef.Setup(j => j.Conditions).Returns(
            new List<(string LeftField, string RightField)> { ("Id", "Id") });

        var left = new List<dynamic> { CreateRecord(new { Id = 1 }) };
        var right = new List<dynamic> { CreateRecord(new { Id = 1 }) };

        // Act & Assert - Will throw because "NonExistentInner" doesn't match Inner/Left/Cross
        Should.Throw<NotSupportedException>(() =>
            _merger.NestedLoopJoin(left, right, mockJoinDef.Object, (l, r) => l).ToList());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoinWithLeftJoinReturnsAllLeftRecords()
    {
        // Arrange
        var left = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "John" }),
            CreateRecord(new { Id = 2, Name = "Jane" }),
            CreateRecord(new { Id = 3, Name = "Bob" })
        };

        var right = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1 })
        };

        var joinDef = new JoinDefinition
        {
            ContainerName = "Orders",
            JoinType = JoinTypes.ByName("Left"),
            Conditions = new List<(string LeftField, string RightField)>
            {
                ("Customers.Id", "Orders.CustomerId")
            }
        };

        // Act
        var results = _merger.HashJoin(left, right, joinDef, MergeRecords).ToList();

        // Assert - left join returns all left records
        results.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    private static JoinDefinition CreateJoinDefinition(string joinType)
    {
        return new JoinDefinition
        {
            ContainerName = "Orders",
            JoinType = JoinTypes.ByName(joinType),
            Conditions = new List<(string LeftField, string RightField)>
            {
                ("Customers.Id", "Orders.CustomerId")
            }
        };
    }

    private static dynamic CreateRecord(object source)
    {
        var expando = new ExpandoObject() as IDictionary<string, object?>;
        foreach (var prop in source.GetType().GetProperties())
        {
            expando[prop.Name] = prop.GetValue(source);
        }
        return expando;
    }

    private static dynamic MergeRecords(dynamic left, dynamic? right)
    {
        var merged = new ExpandoObject() as IDictionary<string, object?>;
        if (left is IDictionary<string, object?> leftDict)
        {
            foreach (var kvp in leftDict)
                merged[kvp.Key] = kvp.Value;
        }
        if (right is IDictionary<string, object?> rightDict)
        {
            foreach (var kvp in rightDict)
                merged[merged.ContainsKey(kvp.Key) ? $"right_{kvp.Key}" : kvp.Key] = kvp.Value;
        }
        return merged;
    }
}
