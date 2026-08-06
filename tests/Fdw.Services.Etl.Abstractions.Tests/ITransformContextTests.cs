using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Fdw.Services.Etl.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for ITransformContext interface contract.
/// </summary>
public class ITransformContextTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExecutionIdPropertyCanBeRead()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var context = new TestTransformContext { ExecutionId = expectedId };

        // Act
        var result = context.ExecutionId;

        // Assert
        result.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void LoggerPropertyCanBeRead()
    {
        // Arrange
        var logger = Mock.Of<ILogger>();
        var context = new TestTransformContext { Logger = logger };

        // Act
        var result = context.Logger;

        // Assert
        result.ShouldBe(logger);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void VariablesPropertyCanBeRead()
    {
        // Arrange
        var variables = new Dictionary<string, object?> { ["key"] = "value" };
        var context = new TestTransformContext { Variables = variables };

        // Act
        var result = context.Variables;

        // Assert
        result.ShouldBe(variables);
        result.Count.ShouldBe(1);
        result["key"].ShouldBe("value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void VariablesPropertyCanBeEmpty()
    {
        // Arrange
        var variables = new Dictionary<string, object?>();
        var context = new TestTransformContext { Variables = variables };

        // Act
        var result = context.Variables;

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CalculationEnginePropertyCanBeRead()
    {
        // Arrange
        var engine = new object();
        var context = new TestTransformContext { CalculationEngine = engine };

        // Act
        var result = context.CalculationEngine;

        // Assert
        result.ShouldBe(engine);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CalculationEnginePropertyCanBeNull()
    {
        // Arrange
        var context = new TestTransformContext { CalculationEngine = null };

        // Act
        var result = context.CalculationEngine;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConnectionProviderPropertyCanBeRead()
    {
        // Arrange
        var provider = new object();
        var context = new TestTransformContext { ConnectionProvider = provider };

        // Act
        var result = context.ConnectionProvider;

        // Assert
        result.ShouldBe(provider);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConnectionProviderPropertyCanBeNull()
    {
        // Arrange
        var context = new TestTransformContext { ConnectionProvider = null };

        // Act
        var result = context.ConnectionProvider;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DataGatewayPropertyCanBeRead()
    {
        // Arrange
        var gateway = new object();
        var context = new TestTransformContext { DataGateway = gateway };

        // Act
        var result = context.DataGateway;

        // Assert
        result.ShouldBe(gateway);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DataGatewayPropertyCanBeNull()
    {
        // Arrange
        var context = new TestTransformContext { DataGateway = null };

        // Act
        var result = context.DataGateway;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ReportErrorCanBeCalledWithError()
    {
        // Arrange
        var context = new TestTransformContext();
        const string error = "Test error";

        // Act
        context.ReportError(error);

        // Assert
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].ShouldBe(error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ReportErrorCanBeCalledWithErrorAndRecord()
    {
        // Arrange
        var context = new TestTransformContext();
        const string error = "Test error";
        var record = new Dictionary<string, object?> { ["field"] = "value" };

        // Act
        context.ReportError(error, record);

        // Assert
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].ShouldBe(error);
        context.Records.Count.ShouldBe(1);
        context.Records[0].ShouldBe(record);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ReportErrorCanBeCalledWithNullRecord()
    {
        // Arrange
        var context = new TestTransformContext();
        const string error = "Test error";

        // Act
        context.ReportError(error, null);

        // Assert
        context.Errors.Count.ShouldBe(1);
        context.Records.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ReportErrorCanBeCalledMultipleTimes()
    {
        // Arrange
        var context = new TestTransformContext();

        // Act
        context.ReportError("Error 1");
        context.ReportError("Error 2");
        context.ReportError("Error 3");

        // Assert
        context.Errors.Count.ShouldBe(3);
    }

    /// <summary>
    /// Test implementation of ITransformContext.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestTransformContext : ITransformContext
    {
        public Guid ExecutionId { get; set; } = Guid.NewGuid();
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
        public CancellationToken CancellationToken { get; set; }
        public ILogger Logger { get; set; } = Mock.Of<ILogger>();
        public IServiceProvider Services { get; set; } = Mock.Of<IServiceProvider>();
        public IReadOnlyDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
        public IDictionary<string, object?> SharedState { get; set; } = new Dictionary<string, object?>();
        public IReadOnlyDictionary<string, object?> Variables { get; set; } = new Dictionary<string, object?>();
        public object? CalculationEngine { get; set; }
        public object? ConnectionProvider { get; set; }
        public object? DataGateway { get; set; }

        public List<string> Errors { get; } = new();
        public List<IDictionary<string, object?>?> Records { get; } = new();

        public void ReportError(string error, IDictionary<string, object?>? record = null)
        {
            Errors.Add(error);
            if (record != null)
            {
                Records.Add(record);
            }
        }
    }
}
