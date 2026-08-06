using Fdw.Collections;
using Fdw.Messages;

namespace Fdw.Messages.Tests;

/// <summary>
/// Tests for the GenericMessage class.
/// </summary>
public class GenericMessageTests
{
    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithDefaults()
    {
        // Act
        var message = new GenericMessage();

        // Assert
        message.Message.ShouldBe(string.Empty);
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Code.ShouldBeNull();
        message.Source.ShouldBeNull();
        message.Id.ShouldBe(1);
        message.Name.ShouldBe("GenericMessage");
        message.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
        message.CorrelationId.ShouldNotBe(Guid.Empty);
        message.Metadata.ShouldNotBeNull();
        message.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithMessage_SetsMessageAndDefaultSeverity()
    {
        // Arrange
        const string messageText = "Test message";

        // Act
        var message = new GenericMessage(messageText);

        // Assert
        message.Message.ShouldBe(messageText);
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Code.ShouldBeNull();
        message.Source.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNullMessage_SetsEmptyString()
    {
        // Act
        var message = new GenericMessage(null!);

        // Assert
        message.Message.ShouldBe(string.Empty);
        message.Severity.ShouldBe(MessageSeverity.Information);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFullDetails_SetsAllProperties()
    {
        // Arrange
        const string messageText = "Error occurred";
        const string code = "ERR001";
        const string source = "TestModule";

        // Act
        var message = new GenericMessage(MessageSeverity.Error, messageText, code, source);

        // Assert
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe(messageText);
        message.Code.ShouldBe(code);
        message.Source.ShouldBe(source);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFullDetailsNullMessage_SetsEmptyString()
    {
        // Act
        var message = new GenericMessage(MessageSeverity.Warning, null!, "CODE", "Source");

        // Assert
        message.Message.ShouldBe(string.Empty);
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Code.ShouldBe("CODE");
        message.Source.ShouldBe("Source");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFullDetailsNullCodeAndSource_AcceptsNulls()
    {
        // Act
        var message = new GenericMessage(MessageSeverity.Critical, "Critical error", null, null);

        // Assert
        message.Message.ShouldBe("Critical error");
        message.Severity.ShouldBe(MessageSeverity.Critical);
        message.Code.ShouldBeNull();
        message.Source.ShouldBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange
        var message = new GenericMessage();
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var correlationId = Guid.NewGuid();
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        message.Message = "Updated message";
        message.Severity = MessageSeverity.Warning;
        message.Code = "WARN001";
        message.Source = "UpdatedSource";
        message.Id = 42;
        message.Name = "CustomName";
        message.Timestamp = timestamp;
        message.CorrelationId = correlationId;
        message.Metadata = metadata;

        // Assert
        message.Message.ShouldBe("Updated message");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Code.ShouldBe("WARN001");
        message.Source.ShouldBe("UpdatedSource");
        message.Id.ShouldBe(42);
        message.Name.ShouldBe("CustomName");
        message.Timestamp.ShouldBe(timestamp);
        message.CorrelationId.ShouldBe(correlationId);
        message.Metadata.ShouldBeSameAs(metadata);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CorrelationId_GeneratesUniqueValues()
    {
        // Act
        var message1 = new GenericMessage();
        var message2 = new GenericMessage();

        // Assert
        message1.CorrelationId.ShouldNotBe(message2.CorrelationId);
        message1.CorrelationId.ShouldNotBe(Guid.Empty);
        message2.CorrelationId.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData(MessageSeverity.Information)]
    [InlineData(MessageSeverity.Warning)]
    [InlineData(MessageSeverity.Error)]
    [InlineData(MessageSeverity.Critical)]
    public void Severity_SupportsAllSeverityLevels(MessageSeverity severity)
    {
        // Arrange & Act
        var message = new GenericMessage(severity, "Test message");

        // Assert
        message.Severity.ShouldBe(severity);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Metadata_UsesOrdinalComparison()
    {
        // Arrange & Act
        var message = new GenericMessage();

        // Assert
        message.Metadata.ShouldNotBeNull();
        message.Metadata.ShouldBeOfType<Dictionary<string, object>>();
        ((Dictionary<string, object>)message.Metadata).Comparer.ShouldBe(StringComparer.Ordinal);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Metadata_CanBeModified()
    {
        // Arrange
        var message = new GenericMessage();

        // Act
        message.Metadata["key1"] = "value1";
        message.Metadata["key2"] = 42;

        // Assert
        message.Metadata.Count.ShouldBe(2);
        message.Metadata["key1"].ShouldBe("value1");
        message.Metadata["key2"].ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IEnumOption_IdProperty_CanBeSetViaInterface()
    {
        // Arrange
        ITypeOption message = new GenericMessage();

        // Act
        ((GenericMessage)message).Id = 100;

        // Assert
        message.Id.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IEnumOption_NameProperty_CanBeSetViaInterface()
    {
        // Arrange
        ITypeOption message = new GenericMessage();

        // Act
        ((GenericMessage)message).Name = "CustomEnumOption";

        // Assert
        message.Name.ShouldBe("CustomEnumOption");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessage_PropertiesAccessible_ViaInterface()
    {
        // Arrange
        IGenericMessage message = new GenericMessage(MessageSeverity.Warning, "Test");

        // Assert
        message.Message.ShouldBe("Test");
        message.Code.ShouldBeNull();
        message.Source.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("CODE123")]
    [InlineData("ERR-001")]
    [InlineData("WARN_42")]
    public void Code_AcceptsDifferentFormats(string code)
    {
        // Arrange & Act
        var message = new GenericMessage(MessageSeverity.Error, "Test", code);

        // Assert
        message.Code.ShouldBe(code);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Module.Service")]
    [InlineData("Controller.Action")]
    [InlineData("MyComponent")]
    public void Source_AcceptsDifferentFormats(string source)
    {
        // Arrange & Act
        var message = new GenericMessage(MessageSeverity.Information, "Test", null, source);

        // Assert
        message.Source.ShouldBe(source);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Timestamp_CanBeSetToCustomValue()
    {
        // Arrange
        var message = new GenericMessage();
        var customTime = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        message.Timestamp = customTime;

        // Assert
        message.Timestamp.ShouldBe(customTime);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Metadata_CanBeReplaced()
    {
        // Arrange
        var message = new GenericMessage();
        var newMetadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 123
        };

        // Act
        message.Metadata = newMetadata;

        // Assert
        message.Metadata.ShouldBeSameAs(newMetadata);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithSeverity_SetsOtherPropertiesToDefaults()
    {
        // Arrange & Act
        var message = new GenericMessage(MessageSeverity.Critical, "Critical error");

        // Assert
        message.Severity.ShouldBe(MessageSeverity.Critical);
        message.Message.ShouldBe("Critical error");
        message.Code.ShouldBeNull();
        message.Source.ShouldBeNull();
        message.Id.ShouldBe(1);
        message.Name.ShouldBe("GenericMessage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_Severity_DefaultsToInformation()
    {
        // Arrange & Act
        var message = new GenericMessage();

        // Assert
        message.Severity.ShouldBe(MessageSeverity.Information);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Metadata_SupportsComplexObjects()
    {
        // Arrange
        var message = new GenericMessage();
        var complexObject = new { Name = "Test", Count = 5 };

        // Act
        message.Metadata["complex"] = complexObject;

        // Assert
        message.Metadata["complex"].ShouldBe(complexObject);
    }

    #endregion
}
