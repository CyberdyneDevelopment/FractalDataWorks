using Fdw.Processors;
using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Processors.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="ProcessorBase{TCommand, TContext, TBase}"/> class.
/// </summary>
public class ProcessorBaseTests
{
    #region Test Types

    private sealed record TestContext(string? Username, string? Password);

    private sealed class TestProcessor : ProcessorBase<string, TestContext, TestProcessor>
    {
        public TestProcessor()
            : base("Test", "Test Processor", "A test processor", new[] { "Username", "Password" })
        {
        }

        public override IGenericResult<string> Process(string command, TestContext context)
        {
            var validationResult = Validate(context);
            if (!validationResult.IsSuccess)
            {
                return GenericResult<string>.Failure(new GenericMessage(validationResult.CurrentMessage ?? "Validation failed"));
            }

            return GenericResult<string>.Success($"{command}:{context.Username}");
        }
    }

    private sealed class NoRequiredPropertiesProcessor : ProcessorBase<string, TestContext, NoRequiredPropertiesProcessor>
    {
        public NoRequiredPropertiesProcessor()
            : base("NoReqs", "No Requirements", "No required properties", Array.Empty<string>())
        {
        }

        public override IGenericResult<string> Process(string command, TestContext context)
        {
            return GenericResult<string>.Success(command);
        }
    }

    private sealed class EmptyProcessor : ProcessorBase<string, TestContext, EmptyProcessor>
    {
        public EmptyProcessor() : base()
        {
        }

        public override IGenericResult<string> Process(string command, TestContext context)
        {
            return GenericResult<string>.Failure(new GenericMessage("Empty processor cannot process"));
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var processor = new TestProcessor();

        // Assert
        processor.Name.ShouldBe("Test");
        processor.DisplayName.ShouldBe("Test Processor");
        processor.Description.ShouldBe("A test processor");
        processor.Category.ShouldBe("Processor");
        processor.RequiredProperties.Count.ShouldBe(2);
        processor.RequiredProperties[0].ShouldBe("Username");
        processor.RequiredProperties[1].ShouldBe("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithCustomCategory_SetsCategory()
    {
        // Arrange & Act
        var processor = new TestProcessor();

        // Assert
        processor.Category.ShouldBe("Processor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EmptyConstructor_CreatesEmptyProcessor()
    {
        // Arrange & Act
        var processor = new EmptyProcessor();

        // Assert
        processor.Name.ShouldBe(string.Empty);
        processor.DisplayName.ShouldBe(string.Empty);
        processor.Description.ShouldBe(string.Empty);
        processor.IsEmpty.ShouldBeTrue();
        processor.RequiredProperties.ShouldBeEmpty();
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsEmpty_ForNormalProcessor_ReturnsFalse()
    {
        // Arrange
        var processor = new TestProcessor();

        // Act
        var result = processor.IsEmpty;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsEmpty_ForEmptyProcessor_ReturnsTrue()
    {
        // Arrange
        var processor = new EmptyProcessor();

        // Act
        var result = processor.IsEmpty;

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Validate Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Validate_WithValidContext_ReturnsSuccess()
    {
        // Arrange
        var processor = new TestProcessor();
        var context = new TestContext("user", "pass");

        // Act
        var result = processor.Validate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Validate_ForNoRequiredProperties_ReturnsSuccess()
    {
        // Arrange
        var processor = new NoRequiredPropertiesProcessor();
        var context = new TestContext(null, null);

        // Act
        var result = processor.Validate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Process Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Process_WithValidContext_ReturnsProcessedCommand()
    {
        // Arrange
        var processor = new TestProcessor();
        var command = "test-command";
        var context = new TestContext("testuser", "testpass");

        // Act
        var result = processor.Process(command, context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test-command:testuser");
    }

    #endregion

    #region GenerateIdFromName Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenerateIdFromName_WithSameName_GeneratesSameId()
    {
        // Arrange
        var processor1 = new TestProcessor();
        var processor2 = new TestProcessor();

        // Act & Assert
        processor1.Id.ShouldBe(processor2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenerateIdFromName_WithDifferentNames_GeneratesDifferentIds()
    {
        // Arrange
        var processor1 = new TestProcessor();
        var processor2 = new NoRequiredPropertiesProcessor();

        // Act & Assert
        processor1.Id.ShouldNotBe(processor2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenerateIdFromName_ReturnsPositiveId()
    {
        // Arrange
        var processor = new TestProcessor();

        // Act & Assert
        processor.Id.ShouldBeGreaterThan(0);
    }

    #endregion

    #region RequiredProperties Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredProperties_ReturnsReadOnlyList()
    {
        // Arrange
        var processor = new TestProcessor();

        // Act
        var properties = processor.RequiredProperties;

        // Assert
        properties.ShouldNotBeNull();
        properties.ShouldBeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredProperties_ForEmptyProcessor_ReturnsEmptyList()
    {
        // Arrange
        var processor = new EmptyProcessor();

        // Act
        var properties = processor.RequiredProperties;

        // Assert
        properties.ShouldBeEmpty();
    }

    #endregion
}
