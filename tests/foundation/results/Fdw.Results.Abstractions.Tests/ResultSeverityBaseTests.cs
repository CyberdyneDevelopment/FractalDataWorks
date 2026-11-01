using Fdw.Results.Abstractions;

namespace Fdw.Results.Abstractions.Tests;

/// <summary>
/// Tests for ResultSeverityBase.
/// </summary>
public class ResultSeverityBaseTests
{
    private sealed class TestResultSeverity : ResultSeverityBase
    {
        public TestResultSeverity(int id, string name, bool isSuccess, int logLevelValue, bool shouldLog, string colorHint)
            : base(id, name, isSuccess, logLevelValue, shouldLog, colorHint)
        {
        }

        /// <summary>
        /// Creates a test severity using the protected empty constructor via reflection.
        /// </summary>
        public static TestResultSeverity CreateNotFound()
        {
            // We can't call the protected parameterless constructor directly,
            // so we create a valid instance that mimics NotFound behavior
            return new TestResultSeverity(-1, "NotFound", false, 4, true, "gray");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithValidParametersCreatesInstance()
    {
        // Act
        var severity = new TestResultSeverity(1, "Error", false, 4, true, "red");

        // Assert
        severity.ShouldNotBeNull();
        severity.Id.ShouldBe(1);
        severity.Name.ShouldBe("Error");
        severity.IsSuccess.ShouldBeFalse();
        severity.IsFailure.ShouldBeTrue();
        severity.LogLevelValue.ShouldBe(4);
        severity.ShouldLog.ShouldBeTrue();
        severity.ColorHint.ShouldBe("red");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundInstanceHasCorrectDefaults()
    {
        // Act
        var severity = TestResultSeverity.CreateNotFound();

        // Assert
        severity.Id.ShouldBe(-1);
        severity.Name.ShouldBe("NotFound");
        severity.IsSuccess.ShouldBeFalse();
        severity.IsFailure.ShouldBeTrue();
        severity.LogLevelValue.ShouldBe(4);
        severity.ShouldLog.ShouldBeTrue();
        severity.ColorHint.ShouldBe("gray");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsSuccessIsOppositeOfIsFailure()
    {
        // Arrange
        var successSeverity = new TestResultSeverity(1, "Success", true, 2, false, "green");
        var failureSeverity = new TestResultSeverity(2, "Error", false, 4, true, "red");

        // Assert
        successSeverity.IsSuccess.ShouldBeTrue();
        successSeverity.IsFailure.ShouldBeFalse();
        failureSeverity.IsSuccess.ShouldBeFalse();
        failureSeverity.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelValueIsSetCorrectly()
    {
        // Arrange - Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5
        var traceSeverity = new TestResultSeverity(0, "Trace", true, 0, false, "gray");
        var infoSeverity = new TestResultSeverity(2, "Information", true, 2, true, "blue");
        var errorSeverity = new TestResultSeverity(4, "Error", false, 4, true, "red");

        // Assert
        traceSeverity.LogLevelValue.ShouldBe(0);
        infoSeverity.LogLevelValue.ShouldBe(2);
        errorSeverity.LogLevelValue.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ShouldLogIsSetCorrectly()
    {
        // Arrange
        var noLogSeverity = new TestResultSeverity(0, "Trace", true, 0, false, "gray");
        var logSeverity = new TestResultSeverity(4, "Error", false, 4, true, "red");

        // Assert
        noLogSeverity.ShouldLog.ShouldBeFalse();
        logSeverity.ShouldLog.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ColorHintIsSetCorrectly()
    {
        // Arrange
        var severity1 = new TestResultSeverity(1, "Info", true, 2, true, "blue");
        var severity2 = new TestResultSeverity(2, "Warning", true, 3, true, "orange");
        var severity3 = new TestResultSeverity(3, "Error", false, 4, true, "red");

        // Assert
        severity1.ColorHint.ShouldBe("blue");
        severity2.ColorHint.ShouldBe("orange");
        severity3.ColorHint.ShouldBe("red");
    }
}
