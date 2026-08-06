namespace Fdw.Validation.Tests;

public sealed class RulesTestModel
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string SafeString { get; set; } = string.Empty;
}

public sealed class RulesTestValidator : AbstractValidator<RulesTestModel>
{
    public RulesTestValidator(int nameMaxLength = 200, int safeStringMaxLength = 4000)
    {
        RuleFor(x => x.Name).IsValidName(nameMaxLength);
        RuleFor(x => x.ConnectionString).IsValidConnectionString();
        RuleFor(x => x.CronExpression).IsValidCronExpression();
        RuleFor(x => x.Id).IsNotEmpty();
        RuleFor(x => x.SafeString).IsSafeString(safeStringMaxLength);
    }
}

public sealed class FdwValidationRulesTests
{
    private readonly RulesTestValidator _validator = new();

    // IsValidName tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidNamePassesWithValidName()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidNameFailsWhenEmpty()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidNameFailsWhenExceedsMaxLength()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = new string('a', 201),
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidNamePassesAtMaxLength()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = new string('a', 200),
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidNameFailsWhenStartsWithNumber()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "1invalid",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidNameFailsWithSpecialChars()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "invalid@name",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // IsValidConnectionString tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidConnectionStringPassesWithValidString()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=test;Database=db;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ConnectionString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidConnectionStringFailsWhenEmpty()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConnectionString);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(";--")]
    [InlineData("'; ")]
    [InlineData("1=1")]
    [InlineData("' OR ")]
    [InlineData("' AND ")]
    [InlineData("xp_")]
    [InlineData("sp_")]
    [InlineData("EXEC ")]
    [InlineData("EXECUTE ")]
    [InlineData("DROP ")]
    [InlineData("DELETE ")]
    [InlineData("INSERT ")]
    [InlineData("UPDATE ")]
    public void IsValidConnectionStringFailsWithSqlInjectionPatterns(string pattern)
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = $"Server=localhost;{pattern}",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConnectionString);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsValidConnectionStringPatternCheckIsCaseInsensitive()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;exec ",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConnectionString);
    }

    // IsValidCronExpression tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionPassesWithFivePartCron()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionPassesWithSixPartCron()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionFailsWhenEmpty()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionFailsWithTooFewParts()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionFailsWithTooManyParts()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionFailsWithInvalidCharacter()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 @ * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionPassesWithSpecialChars()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "*/5 0-23 L,W ? #1",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CronExpression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsValidCronExpressionPassesWithCommaList()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0,15,30,45 * * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CronExpression);
    }

    // IsNotEmpty (Guid) tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsNotEmptyPassesWithValidGuid()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsNotEmptyFailsWithEmptyGuid()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.Empty,
            SafeString = "Safe text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    // IsSafeString tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSafeStringPassesWithNormalText()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "This is normal text"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSafeStringPassesWithEmptyString()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = ""
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSafeStringPassesWithNewline()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Line1\nLine2"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSafeStringPassesWithTab()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Col1\tCol2"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsSafeStringFailsWithControlCharacter()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Text\0null"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsSafeStringFailsWithBellCharacter()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = "Text\abell"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSafeStringFailsWhenExceedsMaxLength()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = new string('a', 4001)
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SafeString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSafeStringPassesAtMaxLength()
    {
        // Arrange
        var model = new RulesTestModel
        {
            Name = "ValidName",
            ConnectionString = "Server=localhost;",
            CronExpression = "0 0 * * *",
            Id = Guid.NewGuid(),
            SafeString = new string('a', 4000)
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SafeString);
    }
}
