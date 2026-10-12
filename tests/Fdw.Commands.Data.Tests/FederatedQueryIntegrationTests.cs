using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

using Fdw.Services.Data;
namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Integration tests for federated query execution across multiple data sources.
/// Tests the complete flow: FederatedQueryCommand → FederatedExecutor → ResultMerger.
/// </summary>
public sealed class FederatedQueryIntegrationTests
{
    private readonly Mock<ILogger<FederatedExecutor>> _mockLogger;
    private readonly Mock<IDataGateway> _mockDataGateway;
    private readonly IResultMerger _resultMerger;
    private readonly IQueryOptimizer _queryOptimizer;
    private readonly FederatedExecutor _executor;

    public FederatedQueryIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<FederatedExecutor>>();
        _mockDataGateway = new Mock<IDataGateway>();
        _resultMerger = new ResultMerger();
        _queryOptimizer = new QueryOptimizer();

        _executor = new FederatedExecutor(
            _mockLogger.Object,
            new MainDataGatewayProvider(_mockDataGateway.Object),
            _resultMerger,
            _queryOptimizer);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_TwoSourcesWithInnerJoin_ShouldReturnMergedResults()
    {
        // Arrange: Setup customer data from source 1
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "John Doe", Email = "john@example.com" }),
            CreateRecord(new { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }),
            CreateRecord(new { Id = 3, Name = "Bob Wilson", Email = "bob@example.com" })
        };

        // Arrange: Setup order data from source 2
        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1, Amount = 150.00, Status = "Completed" }),
            CreateRecord(new { OrderId = 101, CustomerId = 1, Amount = 75.50, Status = "Pending" }),
            CreateRecord(new { OrderId = 102, CustomerId = 2, Amount = 200.00, Status = "Completed" })
        };

        // Arrange: Mock data gateway responses
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

        // Arrange: Create federated query command
        var command = new FederatedQueryCommand<dynamic>("CustomerOrders")
        {
            Sources = new List<IDataSource>
            {
                new DataSource
                {
                    Name = "Customers",
                    ContainerName = "Customers",
                    ConnectionName = "SqlServerConnection"
                },
                new DataSource
                {
                    Name = "Orders",
                    ContainerName = "Orders",
                    ConnectionName = "SqlServerConnection"
                }
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
            Strategy = new ParallelStrategy()
        };

        // Act: Execute federated query
        var result = await _executor.Execute<dynamic>(command, CancellationToken.None);

        // Assert: Verify success
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var mergedRecords = result.Value.ToList();

        // Assert: Should have 3 records (customer 1 has 2 orders, customer 2 has 1 order)
        mergedRecords.Count.ShouldBe(3);

        // Assert: Verify merged data contains fields from both sources
        var firstRecord = mergedRecords[0] as IDictionary<string, object?>;
        firstRecord.ShouldNotBeNull();
        firstRecord.ContainsKey("Id").ShouldBeTrue();
        firstRecord.ContainsKey("Name").ShouldBeTrue();
        firstRecord.ContainsKey("OrderId").ShouldBeTrue();
        firstRecord.ContainsKey("Amount").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ThreeSourcesWithMultipleJoins_ShouldReturnMergedResults()
    {
        // Arrange: Customer data
        var customerData = new List<dynamic>
        {
            CreateRecord(new { CustomerId = 1, CustomerName = "Alice" }),
            CreateRecord(new { CustomerId = 2, CustomerName = "Bob" })
        };

        // Arrange: Order data
        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1, ProductId = 10 }),
            CreateRecord(new { OrderId = 101, CustomerId = 2, ProductId = 20 })
        };

        // Arrange: Product data
        var productData = new List<dynamic>
        {
            CreateRecord(new { ProductId = 10, ProductName = "Widget", Price = 25.99 }),
            CreateRecord(new { ProductId = 20, ProductName = "Gadget", Price = 45.99 })
        };

        // Arrange: Mock gateway responses
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

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Products"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(productData));

        // Arrange: Create federated query with two joins
        var command = new FederatedQueryCommand<dynamic>("CustomerOrderProducts")
        {
            Sources = new List<IDataSource>
            {
                new DataSource
                {
                    Name = "Customers",
                    ContainerName = "Customers",
                    ConnectionName = "Connection1"
                },
                new DataSource
                {
                    Name = "Orders",
                    ContainerName = "Orders",
                    ConnectionName = "Connection2"
                },
                new DataSource
                {
                    Name = "Products",
                    ContainerName = "Products",
                    ConnectionName = "Connection3"
                }
            },
            JoinDefinitions = new List<IJoinDefinition>
            {
                new JoinDefinition
                {
                    ContainerName = "Orders",
                    JoinType = new InnerJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Customers.CustomerId", "Orders.CustomerId")
                    }
                },
                new JoinDefinition
                {
                    ContainerName = "Products",
                    JoinType = new InnerJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Orders.ProductId", "Products.ProductId")
                    }
                }
            },
            Strategy = new ParallelStrategy()
        };

        // Act
        var result = await _executor.Execute<dynamic>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var mergedRecords = result.Value.ToList();
        mergedRecords.Count.ShouldBe(2); // 2 customers × 1 order each × 1 product each

        // Verify all three sources are merged
        var firstRecord = mergedRecords[0] as IDictionary<string, object?>;
        firstRecord.ShouldNotBeNull();
        firstRecord.ContainsKey("CustomerName").ShouldBeTrue();
        firstRecord.ContainsKey("OrderId").ShouldBeTrue();
        firstRecord.ContainsKey("ProductName").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_LeftJoin_ShouldIncludeNonMatchingLeftRecords()
    {
        // Arrange: Customers (3 customers)
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Alice" }),
            CreateRecord(new { Id = 2, Name = "Bob" }),
            CreateRecord(new { Id = 3, Name = "Charlie" }) // No orders
        };

        // Arrange: Orders (only for customers 1 and 2)
        var orderData = new List<dynamic>
        {
            CreateRecord(new { OrderId = 100, CustomerId = 1 }),
            CreateRecord(new { OrderId = 101, CustomerId = 2 })
        };

        // Arrange: Mock responses
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

        // Arrange: Federated query with LEFT JOIN
        var command = new FederatedQueryCommand<dynamic>("CustomersWithOrders")
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
                    JoinType = new LeftJoinType(),
                    Conditions = new List<(string LeftField, string RightField)>
                    {
                        ("Customers.Id", "Orders.CustomerId")
                    }
                }
            },
            Strategy = new SequentialStrategy()
        };

        // Act
        var result = await _executor.Execute<dynamic>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var mergedRecords = result.Value.ToList();

        // Left join should include all 3 customers (Charlie with null order fields)
        mergedRecords.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_SourceQueryFails_ShouldReturnFailure()
    {
        // Arrange: First source succeeds
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Alice" })
        };

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Customers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(customerData));

        // Arrange: Second source fails
        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Orders"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Failure(new GenericMessage("Database connection failed")));

        // Arrange: Command
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
            }
        };

        // Act
        var result = await _executor.Execute<dynamic>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("Database connection failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_SingleSource_ShouldReturnResultsWithoutJoin()
    {
        // Arrange: Single source
        var customerData = new List<dynamic>
        {
            CreateRecord(new { Id = 1, Name = "Alice" }),
            CreateRecord(new { Id = 2, Name = "Bob" })
        };

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Customers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<dynamic>>.Success(customerData));

        // Arrange: Federated query with single source (no joins)
        var command = new FederatedQueryCommand<dynamic>("Customers")
        {
            Sources = new List<IDataSource>
            {
                new DataSource { Name = "Customers", ContainerName = "Customers", ConnectionName = "Conn1" }
            },
            JoinDefinitions = new List<IJoinDefinition>() // No joins
        };

        // Act
        var result = await _executor.Execute<dynamic>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var records = result.Value.ToList();
        records.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ParallelStrategy_ShouldExecuteSourcesInParallel()
    {
        // Arrange: Multiple sources
        var source1Data = new List<dynamic> { CreateRecord(new { Id = 1 }) };
        var source2Data = new List<dynamic> { CreateRecord(new { Id = 2 }) };
        var source3Data = new List<dynamic> { CreateRecord(new { Id = 3 }) };

        var executionOrder = new List<string>();
        var executionLock = new object();

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Source1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                lock (executionLock) { executionOrder.Add("Source1"); }
                return GenericResult<IEnumerable<dynamic>>.Success(source1Data);
            });

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Source2"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                lock (executionLock) { executionOrder.Add("Source2"); }
                return GenericResult<IEnumerable<dynamic>>.Success(source2Data);
            });

        _mockDataGateway
            .Setup(gw => gw.Execute<IEnumerable<dynamic>>(
                It.Is<IDataCommand>(cmd => cmd.ContainerName == "Source3"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                lock (executionLock) { executionOrder.Add("Source3"); }
                return GenericResult<IEnumerable<dynamic>>.Success(source3Data);
            });

        // Arrange: Command with parallel strategy
        var command = new FederatedQueryCommand<dynamic>("MultiSource")
        {
            Sources = new List<IDataSource>
            {
                new DataSource { Name = "Source1", ContainerName = "Source1", ConnectionName = "Conn1" },
                new DataSource { Name = "Source2", ContainerName = "Source2", ConnectionName = "Conn2" },
                new DataSource { Name = "Source3", ContainerName = "Source3", ConnectionName = "Conn3" }
            },
            JoinDefinitions = new List<IJoinDefinition>(),
            Strategy = new ParallelStrategy() // Parallel execution
        };

        // Act
        var result = await _executor.Execute<dynamic>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify all sources were executed (order may vary due to parallelism)
        executionOrder.Count.ShouldBe(3);
        executionOrder.ShouldContain("Source1");
        executionOrder.ShouldContain("Source2");
        executionOrder.ShouldContain("Source3");
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
