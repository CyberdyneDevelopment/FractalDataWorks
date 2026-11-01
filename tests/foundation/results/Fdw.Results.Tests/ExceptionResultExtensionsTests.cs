using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Results.Tests;

/// <summary>
/// Tests for the ExceptionResultExtensions.FlattenException method.
/// </summary>
public class ExceptionResultExtensionsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ExceptionResultExtensions.FlattenException(null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_SimpleException_ReturnsSingleMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("test");

        // Act
        var messages = ExceptionResultExtensions.FlattenException(exception);

        // Assert
        messages.Count.ShouldBe(1);
        messages[0].Message.ShouldBe("test");
        messages[0].Code.ShouldBe("InvalidOperationException");
        var genericMessage = messages[0].ShouldBeOfType<GenericMessage>();
        genericMessage.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_ExceptionWithInnerException_ReturnsTwoMessages()
    {
        // Arrange
        var inner = new ArgumentException("inner error");
        var outer = new InvalidOperationException("outer error", inner);

        // Act
        var messages = ExceptionResultExtensions.FlattenException(outer);

        // Assert
        messages.Count.ShouldBe(2);
        messages[0].Message.ShouldBe("outer error");
        messages[0].Code.ShouldBe("InvalidOperationException");
        messages[1].Message.ShouldBe("inner error");
        messages[1].Code.ShouldBe("ArgumentException");
        var outerMessage = messages[0].ShouldBeOfType<GenericMessage>();
        outerMessage.Severity.ShouldBe(MessageSeverity.Error);
        var innerMessage = messages[1].ShouldBeOfType<GenericMessage>();
        innerMessage.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_DeepChain_ReturnsAllMessages()
    {
        // Arrange
        var level3 = new FormatException("level 3 error");
        var level2 = new ArgumentException("level 2 error", level3);
        var level1 = new InvalidOperationException("level 1 error", level2);

        // Act
        var messages = ExceptionResultExtensions.FlattenException(level1);

        // Assert
        messages.Count.ShouldBe(3);
        messages[0].Message.ShouldBe("level 1 error");
        messages[0].Code.ShouldBe("InvalidOperationException");
        messages[1].Message.ShouldBe("level 2 error");
        messages[1].Code.ShouldBe("ArgumentException");
        messages[2].Message.ShouldBe("level 3 error");
        messages[2].Code.ShouldBe("FormatException");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_AggregateException_FlattensAllInnerExceptions()
    {
        // Arrange
        var ex1 = new InvalidOperationException("error 1");
        var ex2 = new ArgumentException("error 2");
        var ex3 = new FormatException("error 3");
        var aggregate = new AggregateException(ex1, ex2, ex3);

        // Act
        var messages = ExceptionResultExtensions.FlattenException(aggregate);

        // Assert
        messages.Count.ShouldBe(3);
        messages[0].Message.ShouldBe("error 1");
        messages[0].Code.ShouldBe("InvalidOperationException");
        messages[1].Message.ShouldBe("error 2");
        messages[1].Code.ShouldBe("ArgumentException");
        messages[2].Message.ShouldBe("error 3");
        messages[2].Code.ShouldBe("FormatException");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_NestedAggregateException_FlattensCompletely()
    {
        // Arrange
        var ex1 = new InvalidOperationException("error 1");
        var ex2 = new ArgumentException("error 2");
        var ex3 = new FormatException("error 3");
        var innerAggregate = new AggregateException(ex1, ex2);
        var outerAggregate = new AggregateException(innerAggregate, ex3);

        // Act
        var messages = ExceptionResultExtensions.FlattenException(outerAggregate);

        // Assert — Flatten() yields direct non-aggregate children before nested aggregate children
        messages.Count.ShouldBe(3);
        messages[0].Message.ShouldBe("error 3");
        messages[0].Code.ShouldBe("FormatException");
        messages[1].Message.ShouldBe("error 1");
        messages[1].Code.ShouldBe("InvalidOperationException");
        messages[2].Message.ShouldBe("error 2");
        messages[2].Code.ShouldBe("ArgumentException");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FlattenException_ExceptionWithNullSource_UsesUnknown()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        // Exception.Source is null when the exception hasn't been thrown

        // Act
        var messages = ExceptionResultExtensions.FlattenException(exception);

        // Assert
        messages.Count.ShouldBe(1);
        messages[0].Source.ShouldBe("Unknown");
    }
}
