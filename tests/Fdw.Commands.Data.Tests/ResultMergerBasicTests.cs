using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Basic unit tests for ResultMerger hash join algorithm.
/// </summary>
public sealed class ResultMergerBasicTests
{
    private readonly ResultMerger _merger = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HashJoin_InnerJoin_ShouldReturnOnlyMatchingRecords()
    {
        // Arrange
        var leftRecords = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "John" }),
            CreateRecord(new { Id = 2, Name = "Jane" }),
            CreateRecord(new { Id = 3, Name = "Bob" })
        };

        var rightRecords = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1, Total = 50.00 }),
            CreateRecord(new { OrderId = 101, CustomerId = 1, Total = 75.00 }),
            CreateRecord(new { OrderId = 102, CustomerId = 2, Total = 100.00 })
        };

        var joinDef = new JoinDefinition
        {
            ContainerName = "Orders",
            JoinType = JoinTypes.ByName("Inner"),
            Conditions = new List<(string LeftField, string RightField)>
            {
                ("Customers.Id", "Orders.CustomerId")
            }
        };

        // Act
        var results = _merger.HashJoin(
            leftRecords,
            rightRecords,
            joinDef,
            MergeRecords).ToList();

        // Assert
        results.Count.ShouldBe(3); // Only customers 1 and 2 have orders
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedLoopJoin_InnerJoin_ShouldReturnMatchingRecords()
    {
        // Arrange
        var leftRecords = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "John" })
        };

        var rightRecords = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1, Total = 50.00 })
        };

        var joinDef = new JoinDefinition
        {
            ContainerName = "Orders",
            JoinType = JoinTypes.ByName("Inner"),
            Conditions = new List<(string LeftField, string RightField)>
            {
                ("Customers.Id", "Orders.CustomerId")
            }
        };

        // Act
        var results = _merger.NestedLoopJoin(
            leftRecords,
            rightRecords,
            joinDef,
            MergeRecords).ToList();

        // Assert
        results.Count.ShouldBe(1);
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

        if (left is IDictionary<string, object?> leftDictionary)
        {
            foreach (var kvp in leftDictionary)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        if (right is IDictionary<string, object?> rightDictionary)
        {
            foreach (var kvp in rightDictionary)
            {
                if (merged.ContainsKey(kvp.Key))
                {
                    merged[$"right_{kvp.Key}"] = kvp.Value;
                }
                else
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }
        }

        return merged;
    }
}
