using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Processors;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionProcessorBase via concrete test implementation.
/// </summary>
public class ConnectionProcessorBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestConnectionProcessor
        : ConnectionProcessorBase<string, TestContext, TestConnectionProcessor>
    {
        public TestConnectionProcessor(string name, string displayName, string description, IReadOnlyList<string> requiredProperties)
            : base(name, displayName, description, requiredProperties)
        {
        }

        public TestConnectionProcessor() : base()
        {
        }

        public override IGenericResult<string> Process(string command, TestContext context)
        {
            return GenericResult<string>.Success("Processed");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestContext
    {
        public Dictionary<string, object> Properties { get; } = new();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorSetsNameCorrectly()
    {
        // Arrange
        var name = "TestProcessor";
        var displayName = "Test Processor";
        var description = "A test processor";
        var requiredProperties = new List<string> { "Property1" };

        // Act
        var processor = new TestConnectionProcessor(name, displayName, description, requiredProperties);

        // Assert
        processor.Name.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorSetsDisplayNameCorrectly()
    {
        // Arrange
        var name = "TestProcessor";
        var displayName = "Test Processor";
        var description = "A test processor";
        var requiredProperties = new List<string> { "Property1" };

        // Act
        var processor = new TestConnectionProcessor(name, displayName, description, requiredProperties);

        // Assert
        processor.DisplayName.ShouldBe(displayName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorSetsDescriptionCorrectly()
    {
        // Arrange
        var name = "TestProcessor";
        var displayName = "Test Processor";
        var description = "A test processor";
        var requiredProperties = new List<string> { "Property1" };

        // Act
        var processor = new TestConnectionProcessor(name, displayName, description, requiredProperties);

        // Assert
        processor.Description.ShouldBe(description);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorSetsCategoryToConnection()
    {
        // Arrange
        var name = "TestProcessor";
        var displayName = "Test Processor";
        var description = "A test processor";
        var requiredProperties = new List<string> { "Property1" };

        // Act
        var processor = new TestConnectionProcessor(name, displayName, description, requiredProperties);

        // Assert
        processor.Category.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorSetsRequiredPropertiesCorrectly()
    {
        // Arrange
        var name = "TestProcessor";
        var displayName = "Test Processor";
        var description = "A test processor";
        var requiredProperties = new List<string> { "Property1", "Property2" };

        // Act
        var processor = new TestConnectionProcessor(name, displayName, description, requiredProperties);

        // Assert
        processor.RequiredProperties.ShouldBe(requiredProperties);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ImplementsIConnectionProcessor()
    {
        // Arrange
        var processor = new TestConnectionProcessor("Test", "Test", "Test", new List<string>());

        // Assert
        processor.ShouldBeAssignableTo<IConnectionProcessor<string, TestContext>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ProcessReturnsSuccess()
    {
        // Arrange
        var processor = new TestConnectionProcessor("Test", "Test", "Test", new List<string>());
        var context = new TestContext();

        // Act
        var result = processor.Process("command", context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Processed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptyConstructorCreatesInstance()
    {
        // Act
        var processor = new TestConnectionProcessor();

        // Assert
        processor.ShouldNotBeNull();
    }
}
