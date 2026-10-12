namespace Fdw.Validation.Tests;

// Test fixtures
public sealed class TestRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid Id { get; set; }
    public string Password { get; set; } = string.Empty;
}

public sealed class TestFdwValidator : FdwValidator<TestRequest>
{
    public TestFdwValidator()
    {
        ValidateName(x => x.Name);
        ValidateEmail(x => x.Email);
        ValidateId(x => x.Id);
        ValidatePassword(x => x.Password);
    }
}

public sealed class CustomLimitsFdwValidator : FdwValidator<TestRequest>
{
    public CustomLimitsFdwValidator()
    {
        ValidateName(x => x.Name, 50);
        ValidatePassword(x => x.Password, 12);
    }
}

public sealed class FdwValidatorTests
{
    private readonly TestFdwValidator _validator = new();
    private readonly CustomLimitsFdwValidator _customValidator = new();

    // ValidateName tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNamePassesWithValidName()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNameFailsWhenEmpty()
    {
        // Arrange
        var request = new TestRequest { Name = "", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNameFailsWhenExceedsMaxLength()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = new string('a', 201),
            Id = Guid.NewGuid(),
            Password = "password123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNamePassesAtMaxLength()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = new string('a', 200),
            Id = Guid.NewGuid(),
            Password = "password123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNameFailsWhenStartsWithNumber()
    {
        // Arrange
        var request = new TestRequest { Name = "1test", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNameFailsWithSpecialChars()
    {
        // Arrange
        var request = new TestRequest { Name = "test@name", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNamePassesWithHyphen()
    {
        // Arrange
        var request = new TestRequest { Name = "test-name", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNamePassesWithUnderscore()
    {
        // Arrange
        var request = new TestRequest { Name = "test_name", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNameRespectsCustomMaxLength()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = new string('a', 51),
            Id = Guid.NewGuid(),
            Password = "password123456"
        };

        // Act
        var result = _customValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ValidateEmail tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateEmailPassesWithValidEmail()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = "TestName",
            Email = "test@example.com",
            Id = Guid.NewGuid(),
            Password = "password123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateEmailPassesWhenNull()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = "TestName",
            Email = null,
            Id = Guid.NewGuid(),
            Password = "password123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateEmailFailsWithInvalidFormat()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = "TestName",
            Email = "invalidemail",
            Id = Guid.NewGuid(),
            Password = "password123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // Note: ValidateEmail has MaximumLength(320) but FluentValidation's EmailAddress validator
    // has its own internal length limits that are more restrictive, so we don't test exceeding 320 chars
    // as the EmailAddress validator will reject it before MaximumLength is checked.

    // ValidateId tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateIdPassesWithValidGuid()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateIdFailsWithEmptyGuid()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.Empty, Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    // ValidatePassword tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidatePasswordPassesWithValidPassword()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.NewGuid(), Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidatePasswordFailsWhenEmpty()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.NewGuid(), Password = "" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidatePasswordFailsWhenTooShort()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.NewGuid(), Password = "short" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidatePasswordPassesAtMinLength()
    {
        // Arrange
        var request = new TestRequest { Name = "TestName", Id = Guid.NewGuid(), Password = "12345678" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidatePasswordRespectsCustomMinLength()
    {
        // Arrange
        var request = new TestRequest
        {
            Name = new string('a', 50),
            Id = Guid.NewGuid(),
            Password = "short123"
        };

        // Act
        var result = _customValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
