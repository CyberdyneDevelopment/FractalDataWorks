using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;
using TypedValidationResult = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleResult;

namespace Fdw.Orchestration.Pipelines.Abstractions.Tests;

public class ValidationResultTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SuccessReturnsValidResult()
    {
        var result = TypedValidationResult.Success();

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBeNull();
        result.Severity.ShouldBeNull();
        result.FieldErrors.ShouldNotBeNull();
        result.FieldErrors.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SuccessWithMessageReturnsValidResultWithMessage()
    {
        var result = TypedValidationResult.Success("All checks passed");

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("All checks passed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void FailureReturnsInvalidResult()
    {
        var result = TypedValidationResult.Failure("Name is required");

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Name is required");
        result.Severity.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void FailureWithFieldErrorsPopulatesFieldErrors()
    {
        var fieldErrors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Name"] = "Name is required",
            ["Email"] = "Invalid email format"
        };

        var result = TypedValidationResult.Failure("Validation failed", fieldErrors);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Validation failed");
        result.FieldErrors.Count.ShouldBe(2);
        result.FieldErrors["Name"].ShouldBe("Name is required");
        result.FieldErrors["Email"].ShouldBe("Invalid email format");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithNullFieldErrorsDefaultsToEmpty()
    {
        var result = new TypedValidationResult(false, "Error", null, null);

        result.FieldErrors.ShouldNotBeNull();
        result.FieldErrors.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorPreservesAllProperties()
    {
        var fieldErrors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Field1"] = "Error1"
        };

        var result = new TypedValidationResult(true, "message", null, fieldErrors);

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("message");
        result.Severity.ShouldBeNull();
        result.FieldErrors.Count.ShouldBe(1);
    }
}

public class SimpleValidationResultTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultValuesAreCorrect()
    {
        var result = new ValidationResult();

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(0);
        result.Warnings.ShouldNotBeNull();
        result.Warnings.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CanSetIsValidToTrue()
    {
        var result = new ValidationResult { IsValid = true };

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CanAddErrors()
    {
        var severity = new Mock<IValidationSeverity>();
        var result = new ValidationResult
        {
            IsValid = false,
            Errors =
            [
                new ValidationError
                {
                    RuleId = "R1",
                    Field = "Name",
                    Message = "Name is required",
                    Severity = severity.Object
                }
            ]
        };

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].RuleId.ShouldBe("R1");
        result.Errors[0].Field.ShouldBe("Name");
        result.Errors[0].Message.ShouldBe("Name is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CanAddWarnings()
    {
        var result = new ValidationResult
        {
            IsValid = true,
            Warnings =
            [
                new ValidationWarning
                {
                    RuleId = "W1",
                    Field = "Description",
                    Message = "Description is too short"
                }
            ]
        };

        result.Warnings.Count.ShouldBe(1);
        result.Warnings[0].RuleId.ShouldBe("W1");
        result.Warnings[0].Field.ShouldBe("Description");
        result.Warnings[0].Message.ShouldBe("Description is too short");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidationErrorActualValueCanBeNull()
    {
        var severity = new Mock<IValidationSeverity>();
        var error = new ValidationError
        {
            RuleId = "R1",
            Field = "Age",
            Message = "Age is required",
            Severity = severity.Object,
            ActualValue = null
        };

        error.ActualValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidationWarningActualValueCanBeSet()
    {
        var warning = new ValidationWarning
        {
            RuleId = "W1",
            Field = "Score",
            Message = "Score is low",
            ActualValue = 42
        };

        warning.ActualValue.ShouldBe(42);
    }
}
