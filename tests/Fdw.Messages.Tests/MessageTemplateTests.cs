using Fdw.Collections;
using Fdw.Messages;

namespace Fdw.Messages.Tests;

public sealed class MessageTemplateTests
{
    // Test message implementations for testing base class functionality
    private sealed class TestMessage : MessageTemplate<MessageSeverity>
    {
        public TestMessage(int id, string name, MessageSeverity severity, string message, string? code = null, string? source = null)
            : base(id, name, severity, message, code, source)
        {
        }

        public TestMessage(int id, string name, MessageSeverity severity, string message, string? code, string? source, IDictionary<string, object?>? details, object? data)
            : base(id, name, severity, message, code, source, details, data)
        {
        }
    }

    private sealed class TestMessageWithSeverityChange : MessageTemplate<MessageSeverity>
    {
        public TestMessageWithSeverityChange(int id, string name, MessageSeverity severity, string message, string? code = null, string? source = null)
            : base(id, name, severity, message, code, source)
        {
        }

        public override MessageTemplate<MessageSeverity> WithSeverity(MessageSeverity severity)
        {
            return new TestMessageWithSeverityChange(Id, Name, severity, Message, Code, Source);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldSetAllProperties_WhenCalledWithBasicParameters()
    {
        // Arrange & Act
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Error,
            message: "Test error message",
            code: "TEST001",
            source: "TestSource");

        // Assert
        message.Id.ShouldBe(1);
        message.Name.ShouldBe("TestMessage");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Test error message");
        message.Code.ShouldBe("TEST001");
        message.Source.ShouldBe("TestSource");
        message.OriginatedIn.ShouldBe("TestSource");
        message.Details.ShouldBeNull();
        message.Data.ShouldBeNull();
        message.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldSetOriginatedInToUnknown_WhenSourceIsNull()
    {
        // Arrange & Act
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Test message",
            code: null,
            source: null);

        // Assert
        message.Source.ShouldBeNull();
        message.OriginatedIn.ShouldBe("Unknown");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldSetDetailsAndData_WhenCalledWithExtendedParameters()
    {
        // Arrange
        var details = new Dictionary<string, object?> { { "Key1", "Value1" }, { "Key2", 42 } };
        var data = new { Property = "Value" };

        // Act
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Warning,
            message: "Test warning",
            code: "WARN001",
            source: "TestSource",
            details: details,
            data: data);

        // Assert
        message.Details.ShouldBe(details);
        message.Data.ShouldBe(data);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldSetOriginatedInToUnknown_WhenSourceIsNullInExtendedConstructor()
    {
        // Arrange
        var details = new Dictionary<string, object?> { { "Key1", "Value1" } };
        var data = new { Property = "Value" };

        // Act
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Test message",
            code: null,
            source: null,
            details: details,
            data: data);

        // Assert
        message.Source.ShouldBeNull();
        message.OriginatedIn.ShouldBe("Unknown");
        message.Details.ShouldBe(details);
        message.Data.ShouldBe(data);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldReturnUnformattedMessage_WhenNoArgumentsProvided()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Simple message without placeholders");

        // Act
        var formatted = message.Format();

        // Assert
        formatted.ShouldBe("Simple message without placeholders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldReturnUnformattedMessage_WhenEmptyArgumentsArrayProvided()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Simple message");

        // Act
        var formatted = message.Format(Array.Empty<object>());

        // Assert
        formatted.ShouldBe("Simple message");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldFormatMessage_WhenArgumentsProvided()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Error,
            message: "Error occurred in {0} at {1}");

        // Act
        var formatted = message.Format("TestMethod", "line 42");

        // Assert
        formatted.ShouldBe("Error occurred in TestMethod at line 42");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldHandleMultipleArguments_WithComplexFormatting()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Warning,
            message: "User {0} attempted {1} at {2:yyyy-MM-dd HH:mm:ss}");

        var timestamp = new DateTime(2025, 1, 15, 10, 30, 0);

        // Act
        var formatted = message.Format("Alice", "login", timestamp);

        // Assert
        formatted.ShouldBe("User Alice attempted login at 2025-01-15 10:30:00");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldReturnUnformattedMessage_WhenNullArgumentsProvided()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Simple message");

        // Act
        var formatted = message.Format(null!);

        // Assert
        formatted.ShouldBe("Simple message");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnTrue_WhenMessagesAreIdentical()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenIdsAreDifferent()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(2, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenNamesAreDifferent()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test1", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(1, "Test2", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenSeveritiesAreDifferent()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Warning, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenMessagesAreDifferent()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message1", "CODE", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message2", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenCodesAreDifferent()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE1", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE2", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenSourcesAreDifferent()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source1");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source2");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnTrue_WhenBothCodesAreNull()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", null, "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", null, "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnTrue_WhenBothSourcesAreNull()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", null);
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", null);

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message.Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var other = new object();

        // Act
        var result = message.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_ShouldReturnSameValue_ForIdenticalMessages()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var hash1 = message1.GetHashCode();
        var hash2 = message2.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_ShouldReturnDifferentValue_ForDifferentMessages()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(2, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var hash1 = message1.GetHashCode();
        var hash2 = message2.GetHashCode();

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldIncludeAllComponents_WithAllPropertiesSet()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Error,
            message: "Test error message",
            code: "TEST001",
            source: "TestSource");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("[Error]");
        result.ShouldContain("(TEST001)");
        result.ShouldContain("Test error message");
        result.ShouldContain("Source: TestSource");
        result.ShouldContain("UTC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldOmitCode_WhenCodeIsNull()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Test message",
            code: null,
            source: "TestSource");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("[Information]");
        result.ShouldNotContain("(");
        result.ShouldContain("Test message");
        result.ShouldContain("Source: TestSource");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldOmitSource_WhenSourceIsNull()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Warning,
            message: "Test warning",
            code: "WARN001",
            source: null);

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("[Warning]");
        result.ShouldContain("(WARN001)");
        result.ShouldContain("Test warning");
        result.ShouldNotContain("Source:");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldOmitCode_WhenCodeIsEmpty()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Information,
            message: "Test message",
            code: "",
            source: "TestSource");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("[Information]");
        result.ShouldNotContain("(");
        result.ShouldContain("Test message");
        result.ShouldContain("Source: TestSource");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldOmitSource_WhenSourceIsEmpty()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Error,
            message: "Test error",
            code: "ERR001",
            source: "");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("[Error]");
        result.ShouldContain("(ERR001)");
        result.ShouldContain("Test error");
        result.ShouldNotContain("Source:");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldFormatTimestamp_InCorrectFormat()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Critical,
            message: "Critical error");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldMatch(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void WithSeverity_ShouldThrowNotSupportedException_ByDefault()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Error, "Message");

        // Act & Assert
        Should.Throw<NotSupportedException>(() => message.WithSeverity(MessageSeverity.Warning))
            .Message.ShouldContain("TestMessage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void WithSeverity_ShouldReturnNewInstanceWithNewSeverity_WhenOverridden()
    {
        // Arrange
        var message = new TestMessageWithSeverityChange(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message.WithSeverity(MessageSeverity.Warning);

        // Assert
        result.ShouldNotBeSameAs(message);
        result.Severity.ShouldBe(MessageSeverity.Warning);
        result.Id.ShouldBe(message.Id);
        result.Name.ShouldBe(message.Name);
        result.Message.ShouldBe(message.Message);
        result.Code.ShouldBe(message.Code);
        result.Source.ShouldBe(message.Source);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData(MessageSeverity.Information)]
    [InlineData(MessageSeverity.Warning)]
    [InlineData(MessageSeverity.Error)]
    [InlineData(MessageSeverity.Critical)]
    public void Constructor_ShouldAcceptAllSeverityLevels(MessageSeverity severity)
    {
        // Arrange & Act
        var message = new TestMessage(1, "Test", severity, "Test message");

        // Assert
        message.Severity.ShouldBe(severity);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenCodeIsNullVsNonNull()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", null, "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldReturnFalse_WhenSourceIsNullVsNonNull()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", null);
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldOmitBothCodeAndSource_WhenBothAreNull()
    {
        // Arrange
        var message = new TestMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Critical,
            message: "Critical error",
            code: null,
            source: null);

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("[Critical]");
        result.ShouldNotContain("(");
        result.ShouldContain("Critical error");
        result.ShouldNotContain("Source:");
        result.ShouldContain("UTC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldInitializeTimestampToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Test message");
        var after = DateTime.UtcNow;

        // Assert
        message.Timestamp.ShouldBeInRange(before, after);
        message.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IMessageIdentifier_PropertiesAccessible_ViaInterface()
    {
        // Arrange
        IMessageIdentifier message = new TestMessage(42, "TestName", MessageSeverity.Warning, "Test");

        // Assert
        message.Id.ShouldBe(42);
        message.Name.ShouldBe("TestName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessage_PropertiesAccessible_ViaInterface()
    {
        // Arrange
        IGenericMessage<MessageSeverity> message = new TestMessage(
            1, "Test", MessageSeverity.Error, "Error message", "ERR001", "TestSource");

        // Assert
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Error message");
        message.Code.ShouldBe("ERR001");
        message.Source.ShouldBe("TestSource");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IEnumOption_PropertiesAccessible_ViaInterface()
    {
        // Arrange
        ITypeOption message = new TestMessage(99, "EnumName", MessageSeverity.Critical, "Critical");

        // Assert
        message.Id.ShouldBe(99);
        message.Name.ShouldBe("EnumName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldHandleNullInArguments()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Value: {0}");

        // Act
        var formatted = message.Format((object)null!);

        // Assert
        formatted.ShouldBe("Value: ");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldHandleNumericFormatting()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Amount: {0:C2}");

        // Act
        var formatted = message.Format(123.456);

        // Assert
        formatted.ShouldContain("123");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_ShouldBeConsistent_ForSameInstance()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var hash1 = message.GetHashCode();
        var hash2 = message.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_ShouldHandleNullCode()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Error, "Message", null, "Source");

        // Act & Assert
        Should.NotThrow(() => message.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_ShouldHandleNullSource()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", null);

        // Act & Assert
        Should.NotThrow(() => message.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldHandleEmptyCode()
    {
        // Arrange & Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message", "", "Source");

        // Assert
        message.Code.ShouldBe("");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldHandleEmptySource()
    {
        // Arrange & Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message", "CODE", "");

        // Assert
        message.Source.ShouldBe("");
        message.OriginatedIn.ShouldBe("");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Details_ShouldBeNullByDefault_InBasicConstructor()
    {
        // Arrange & Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message");

        // Assert
        message.Details.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Data_ShouldBeNullByDefault_InBasicConstructor()
    {
        // Arrange & Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message");

        // Assert
        message.Data.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ExtendedConstructor_ShouldAcceptNullDetails()
    {
        // Arrange & Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message", null, null, null, null);

        // Assert
        message.Details.ShouldBeNull();
        message.Data.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ExtendedConstructor_ShouldAcceptNonNullData()
    {
        // Arrange
        var data = new { Key = "Value" };

        // Act
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message", null, null, null, data);

        // Assert
        message.Data.ShouldBe(data);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldUseOrdinalComparison_ForStrings()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "message", "CODE", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "MESSAGE", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldHandleWhitespaceInMessage()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "  Multiple   Spaces  ");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldContain("  Multiple   Spaces  ");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void WithSeverity_ShouldMaintainOtherProperties_WhenOverridden()
    {
        // Arrange
        var original = new TestMessageWithSeverityChange(5, "Original", MessageSeverity.Information, "Info message", "INFO", "Source");

        // Act
        var updated = original.WithSeverity(MessageSeverity.Critical);

        // Assert
        updated.Id.ShouldBe(5);
        updated.Name.ShouldBe("Original");
        updated.Message.ShouldBe("Info message");
        updated.Code.ShouldBe("INFO");
        updated.Source.ShouldBe("Source");
        updated.Severity.ShouldBe(MessageSeverity.Critical);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Format_ShouldUseInvariantCulture()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Date: {0:d}");
        var date = new DateTime(2025, 10, 20);

        // Act
        var formatted = message.Format(date);

        // Assert
        formatted.ShouldBe("Date: 10/20/2025");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_ShouldCompareEnumValues_NotReferences()
    {
        // Arrange
        var message1 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");
        var message2 = new TestMessage(1, "Test", MessageSeverity.Error, "Message", "CODE", "Source");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldAcceptZeroId()
    {
        // Arrange & Act
        var message = new TestMessage(0, "Test", MessageSeverity.Information, "Message");

        // Assert
        message.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ShouldAcceptNegativeId()
    {
        // Arrange & Act
        var message = new TestMessage(-1, "Test", MessageSeverity.Information, "Message");

        // Assert
        message.Id.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ShouldIncludeTimestamp_WithCorrectFormat()
    {
        // Arrange
        var message = new TestMessage(1, "Test", MessageSeverity.Information, "Message");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldMatch(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC$");
    }
}
