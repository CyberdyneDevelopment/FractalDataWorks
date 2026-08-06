namespace Fdw.Validation.Tests;

public sealed class ValidationResultExtensionsTests
{
    // ToGenericResult (non-generic) tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsSuccessWhenValid()
    {
        // Arrange
        var validationResult = new ValidationResult();

        // Act
        var result = validationResult.ToGenericResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsFailureWithErrors()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Email", "Email is invalid")
        });

        // Act
        var result = validationResult.ToGenericResult();

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Messages.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultIncludesPropertyNameInMessage()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required")
        });

        // Act
        var result = validationResult.ToGenericResult();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var message = result.Messages.First();
        message.Message.ShouldContain("Name:");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultSetsMessageSeverityToError()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required")
        });

        // Act
        var result = validationResult.ToGenericResult();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var message = (GenericMessage)result.Messages.First();
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultSetsCodeToValidation()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required")
        });

        // Act
        var result = validationResult.ToGenericResult();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var message = result.Messages.First();
        message.Code.ShouldNotBeNull();
        message.Code.ShouldContain("VALIDATION");
    }

    // ToGenericResult<T> (generic) tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsSuccessWithValue()
    {
        // Arrange
        var validationResult = new ValidationResult();
        var value = "TestValue";

        // Act
        var result = validationResult.ToGenericResult(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsFailureWithoutValue()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required")
        });
        var value = "TestValue";

        // Act
        var result = validationResult.ToGenericResult(value);

        // Assert
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => { var _ = result.Value; });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultIncludesErrorMessages()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Port", "Port must be greater than 0")
        });
        var value = "TestValue";

        // Act
        var result = validationResult.ToGenericResult(value);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Messages.Count.ShouldBe(2);
        result.Messages.ShouldContain(m => m.Message.Contains("Name:"));
        result.Messages.ShouldContain(m => m.Message.Contains("Port:"));
    }
}
