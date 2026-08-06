using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Tests for post-join filter evaluation in federated queries.
/// </summary>
public sealed class PostJoinFilterTests
{
    private readonly Mock<ILogger<DataGatewayService>> _mockLogger;
    private readonly Mock<IDataConnectionProvider> _mockConnectionProvider;
    private readonly Dictionary<string, IStorageContainer> _containers;
    private readonly Mock<IDataSetProvider> _mockDataSetProvider;
    private readonly PredicatePushdownAnalyzer _predicatePushdownAnalyzer;
    private readonly IResultMerger _resultMerger;
    private readonly IQueryOptimizer _queryOptimizer;
    private readonly DataGatewayService _dataGateway;

    public PostJoinFilterTests()
    {
        _mockLogger = new Mock<ILogger<DataGatewayService>>();
        _mockConnectionProvider = new Mock<IDataConnectionProvider>();
        _containers = new Dictionary<string, IStorageContainer>(StringComparer.OrdinalIgnoreCase);
        _mockDataSetProvider = new Mock<IDataSetProvider>();
        _predicatePushdownAnalyzer = new PredicatePushdownAnalyzer(new Mock<ILogger<PredicatePushdownAnalyzer>>().Object);
        _resultMerger = new ResultMerger();
        _queryOptimizer = new QueryOptimizer();

        _dataGateway = new DataGatewayService(
            _mockLogger.Object,
            _mockConnectionProvider.Object,
            _containers,
            _mockDataSetProvider.Object,
            _predicatePushdownAnalyzer,
            _resultMerger,
            _queryOptimizer);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PostJoinFilter_EqualOperator_ShouldFilterCorrectly()
    {
        // Arrange
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Alice", Status = "Active" }),
            CreateRecord(new { Id = 2, Name = "Bob", Status = "Inactive" }),
            CreateRecord(new { Id = 3, Name = "Charlie", Status = "Active" })
        };

        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1, Total = 50.0 }),
            CreateRecord(new { OrderId = 101, CustomerId = 2, Total = 75.0 }),
            CreateRecord(new { OrderId = 102, CustomerId = 3, Total = 100.0 })
        };

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Customers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(customerData));

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Orders"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(orderData));

        // Create filter: WHERE Status = 'Active'
        var filter = new FilterExpression
        {
            Root = new SimpleFilterCondition
            {
                PropertyName = "Status",
                Operator = new EqualOperator(),
                Value = "Active"
            }
        };

        var command = new FederatedQueryCommand<dynamic>("CustomerOrders")
        {
            Sources = new List<IDataSource>
            {
                new DataSource { Name = "Customers", ContainerName = "Customers", ConnectionName = "Conn1" },
                new DataSource { Name = "Orders", ContainerName = "Orders", ConnectionName = "Conn2" }
            },
            JoinDefinitions = new List<IJoinDefinition>
            {
                new JoinDefinition
                {
                    ContainerName = "Orders",
                    JoinType = new InnerJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Customers.Id", "Orders.CustomerId")
                    }
                }
            },
            Filter = filter
        };

        // Act
        var result = await _dataGateway.Execute<IEnumerable<dynamic>>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var records = result.Value!.ToList();
        records.Count.ShouldBe(2); // Only Active customers (Alice and Charlie)

        var record1 = records[0] as IDictionary<string, object?>;
        record1.ShouldNotBeNull();
        record1["Status"].ShouldBe("Active");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PostJoinFilter_GreaterThanOperator_ShouldFilterNumericValues()
    {
        // Arrange
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Alice" }),
            CreateRecord(new { Id = 2, Name = "Bob" })
        };

        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1, Total = 50.0 }),
            CreateRecord(new { OrderId = 101, CustomerId = 1, Total = 150.0 }),
            CreateRecord(new { OrderId = 102, CustomerId = 2, Total = 75.0 })
        };

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Customers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(customerData));

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Orders"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(orderData));

        // Create filter: WHERE Total > 100
        var filter = new FilterExpression
        {
            Root = new SimpleFilterCondition
            {
                PropertyName = "Total",
                Operator = new GreaterThanOperator(),
                Value = 100.0
            }
        };

        var command = new FederatedQueryCommand<dynamic>("CustomerOrders")
        {
            Sources = new List<IDataSource>
            {
                new DataSource { Name = "Customers", ContainerName = "Customers", ConnectionName = "Conn1" },
                new DataSource { Name = "Orders", ContainerName = "Orders", ConnectionName = "Conn2" }
            },
            JoinDefinitions = new List<IJoinDefinition>
            {
                new JoinDefinition
                {
                    ContainerName = "Orders",
                    JoinType = new InnerJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Customers.Id", "Orders.CustomerId")
                    }
                }
            },
            Filter = filter
        };

        // Act
        var result = await _dataGateway.Execute<IEnumerable<dynamic>>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var records = result.Value!.ToList();
        records.Count.ShouldBe(1); // Only order with Total > 100 (OrderId 101)

        var record1 = records[0] as IDictionary<string, object?>;
        record1.ShouldNotBeNull();
        record1["Total"].ShouldBe(150.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PostJoinFilter_ContainsOperator_ShouldFilterStringValues()
    {
        // Arrange
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Acme Corporation" }),
            CreateRecord(new { Id = 2, Name = "Bob's Hardware" }),
            CreateRecord(new { Id = 3, Name = "Corporation Inc" })
        };

        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1 }),
            CreateRecord(new { OrderId = 101, CustomerId = 2 }),
            CreateRecord(new { OrderId = 102, CustomerId = 3 })
        };

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Customers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(customerData));

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Orders"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(orderData));

        // Create filter: WHERE Name CONTAINS 'Corporation'
        var filter = new FilterExpression
        {
            Root = new SimpleFilterCondition
            {
                PropertyName = "Name",
                Operator = new ContainsOperator(),
                Value = "Corporation"
            }
        };

        var command = new FederatedQueryCommand<dynamic>("CustomerOrders")
        {
            Sources = new List<IDataSource>
            {
                new DataSource { Name = "Customers", ContainerName = "Customers", ConnectionName = "Conn1" },
                new DataSource { Name = "Orders", ContainerName = "Orders", ConnectionName = "Conn2" }
            },
            JoinDefinitions = new List<IJoinDefinition>
            {
                new JoinDefinition
                {
                    ContainerName = "Orders",
                    JoinType = new InnerJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Customers.Id", "Orders.CustomerId")
                    }
                }
            },
            Filter = filter
        };

        // Act
        var result = await _dataGateway.Execute<IEnumerable<dynamic>>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var records = result.Value!.ToList();
        records.Count.ShouldBe(2); // Acme Corporation and Corporation Inc
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PostJoinFilter_FilterGroup_AndLogic_ShouldFilterCorrectly()
    {
        // Arrange
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Alice", Status = "Active", CreditLimit = 5000.0 }),
            CreateRecord(new { Id = 2, Name = "Bob", Status = "Active", CreditLimit = 1000.0 }),
            CreateRecord(new { Id = 3, Name = "Charlie", Status = "Inactive", CreditLimit = 5000.0 })
        };

        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1 }),
            CreateRecord(new { OrderId = 101, CustomerId = 2 }),
            CreateRecord(new { OrderId = 102, CustomerId = 3 })
        };

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Customers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(customerData));

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Orders"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(orderData));

        // Create filter: WHERE Status = 'Active' AND CreditLimit > 2000
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = new List<IFilterNode>
                {
                    new SimpleFilterCondition
                    {
                        PropertyName = "Status",
                        Operator = new EqualOperator(),
                        Value = "Active"
                    },
                    new SimpleFilterCondition
                    {
                        PropertyName = "CreditLimit",
                        Operator = new GreaterThanOperator(),
                        Value = 2000.0
                    }
                }
            }
        };

        var command = new FederatedQueryCommand<dynamic>("CustomerOrders")
        {
            Sources = new List<IDataSource>
            {
                new DataSource { Name = "Customers", ContainerName = "Customers", ConnectionName = "Conn1" },
                new DataSource { Name = "Orders", ContainerName = "Orders", ConnectionName = "Conn2" }
            },
            JoinDefinitions = new List<IJoinDefinition>
            {
                new JoinDefinition
                {
                    ContainerName = "Orders",
                    JoinType = new InnerJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Customers.Id", "Orders.CustomerId")
                    }
                }
            },
            Filter = filter
        };

        // Act
        var result = await _dataGateway.Execute<IEnumerable<dynamic>>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var records = result.Value!.ToList();
        records.Count.ShouldBe(1); // Only Alice (Active AND CreditLimit > 2000)

        var record1 = records[0] as IDictionary<string, object?>;
        record1.ShouldNotBeNull();
        record1["Name"].ShouldBe("Alice");
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
}

/// <summary>
/// Simple filter condition implementation for testing.
/// </summary>
public sealed record SimpleFilterCondition : IFilterCondition, IFilterNode
{
    public required string PropertyName { get; init; }
    public required IFilterOperator Operator { get; init; }
    public object? Value { get; init; }
}
