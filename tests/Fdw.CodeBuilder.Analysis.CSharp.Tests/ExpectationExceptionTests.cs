namespace Fdw.CodeBuilder.Analysis.CSharp.Tests;

public class ExpectationExceptionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new ExpectationException(message);

        // Assert
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe(message);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test message";
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new ExpectationException(message, innerException);

        // Assert
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Exception_CanBeThrown()
    {
        // Arrange & Act & Assert
        Should.Throw<ExpectationException>(() =>
        {
            throw new ExpectationException("Test");
        });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Exception_CanBeCaught()
    {
        // Arrange
        var caught = false;

        // Act
        try
        {
            throw new ExpectationException("Test");
        }
        catch (ExpectationException)
        {
            caught = true;
        }

        // Assert
        caught.ShouldBeTrue();
    }
}
