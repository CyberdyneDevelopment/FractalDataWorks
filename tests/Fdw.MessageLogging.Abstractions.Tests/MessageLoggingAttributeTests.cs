using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.MessageLogging.Abstractions.Tests;

/// <summary>
/// Tests for MessageLoggingAttribute.
/// </summary>
public class MessageLoggingAttributeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithDefaultValues_CreatesInstance()
    {
        // Act
        var attribute = new MessageLoggingAttribute();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.EventId.ShouldBe(-1); // -1 means unset, will be auto-generated
        attribute.Level.ShouldBe(LogLevel.None); // None means use default level
        attribute.Message.ShouldBe(string.Empty);
        attribute.EventName.ShouldBeNull();
        attribute.SkipEnabledCheck.ShouldBeFalse();
        attribute.Severity.ShouldBe(MessageSeverity.Information);
        attribute.AutoMapSeverity.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithEventIdLevelMessage_SetsProperties()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute(5001, LogLevel.Error, "Test message");

        // Assert
        attribute.EventId.ShouldBe(5001);
        attribute.Level.ShouldBe(LogLevel.Error);
        attribute.Message.ShouldBe("Test message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithLevelAndMessage_SetsProperties()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute(LogLevel.Warning, "Warning message");

        // Assert
        attribute.EventId.ShouldBe(-1); // Not set, default
        attribute.Level.ShouldBe(LogLevel.Warning);
        attribute.Message.ShouldBe("Warning message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithLevelOnly_SetsLevel()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute(LogLevel.Critical);

        // Assert
        attribute.EventId.ShouldBe(-1);
        attribute.Level.ShouldBe(LogLevel.Critical);
        attribute.Message.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithMessageOnly_SetsMessage()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute("Test message template");

        // Assert
        attribute.EventId.ShouldBe(-1);
        attribute.Level.ShouldBe(LogLevel.None);
        attribute.Message.ShouldBe("Test message template");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EventId_CanBeSet()
    {
        // Arrange
        var attribute = new MessageLoggingAttribute { EventId = 1234 };

        // Act & Assert
        attribute.EventId.ShouldBe(1234);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EventName_CanBeSet()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute { EventName = "ConnectionFailed" };

        // Assert
        attribute.EventName.ShouldBe("ConnectionFailed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Level_CanBeSet()
    {
        // Arrange
        var attribute = new MessageLoggingAttribute { Level = LogLevel.Error };

        // Act & Assert
        attribute.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_CanBeSet()
    {
        // Arrange
        const string message = "Test message with {parameter}";
        var attribute = new MessageLoggingAttribute { Message = message };

        // Act & Assert
        attribute.Message.ShouldBe(message);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SkipEnabledCheck_CanBeSet()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute { SkipEnabledCheck = true };

        // Assert
        attribute.SkipEnabledCheck.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Severity_CanBeSet()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute { Severity = MessageSeverity.Error };

        // Assert
        attribute.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AutoMapSeverity_CanBeSetToFalse()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute { AutoMapSeverity = false };

        // Assert
        attribute.AutoMapSeverity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var attribute = new MessageLoggingAttribute
        {
            EventId = 5001,
            EventName = "OperationFailed",
            Level = LogLevel.Warning,
            Message = "Warning: {operation} failed",
            SkipEnabledCheck = true,
            Severity = MessageSeverity.Warning,
            AutoMapSeverity = false
        };

        // Assert
        attribute.EventId.ShouldBe(5001);
        attribute.EventName.ShouldBe("OperationFailed");
        attribute.Level.ShouldBe(LogLevel.Warning);
        attribute.Message.ShouldBe("Warning: {operation} failed");
        attribute.SkipEnabledCheck.ShouldBeTrue();
        attribute.Severity.ShouldBe(MessageSeverity.Warning);
        attribute.AutoMapSeverity.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.None)]
    public void Level_SupportsAllLogLevels(LogLevel level)
    {
        // Arrange
        var attribute = new MessageLoggingAttribute { Level = level };

        // Act & Assert
        attribute.Level.ShouldBe(level);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData(MessageSeverity.Information)]
    [InlineData(MessageSeverity.Warning)]
    [InlineData(MessageSeverity.Error)]
    public void Severity_SupportsAllSeverityLevels(MessageSeverity severity)
    {
        // Arrange
        var attribute = new MessageLoggingAttribute { Severity = severity };

        // Act & Assert
        attribute.Severity.ShouldBe(severity);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_IsCorrect()
    {
        // Assert
        var attributeUsage = typeof(MessageLoggingAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Method);
        attributeUsage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_InheritsFromAttribute()
    {
        // Assert
        typeof(Attribute).IsAssignableFrom(typeof(MessageLoggingAttribute)).ShouldBeTrue();
    }
}
