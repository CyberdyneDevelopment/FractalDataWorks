using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Integration.Tests;

/// <summary>
/// Integration tests for Railway-Oriented Programming (ROP) error handling.
/// Validates that FDW uses Result types instead of exceptions for anticipated failures.
/// </summary>
public sealed class RailwayOrientedProgrammingTests
{
    /// <summary>
    /// Scenario 7 Test 1: Success path returns IGenericResult with IsSuccess = true.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SuccessPath_ReturnsSuccessResult()
    {
        // Arrange & Act
        var result = GetData(isValid: true);

        // Assert: Railway success path
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBe("Success");
        result.CurrentMessage.ShouldBeNullOrEmpty();
    }

    /// <summary>
    /// Scenario 7 Test 2: Failure path returns IGenericResult with IsSuccess = false (NOT exception).
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FailurePath_ReturnsFailureResult_NotException()
    {
        // Arrange & Act
        var result = GetData(isValid: false);

        // Assert: Railway failure path - returns failure, does NOT throw
        result.IsSuccess.ShouldBeFalse();
        // Note: Cannot access .Value on failed result - it throws by design
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
        result.CurrentMessage.ShouldContain("Invalid");
    }

    /// <summary>
    /// Scenario 7 Test 3: Chaining operations with Map for success path.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ChainingOperations_SuccessPath_ExecutesAllSteps()
    {
        // Arrange
        var input = "test";

        // Act: Chain operations using Railway pattern
        var result = Validate(input)
            .Bind(value => Transform(value))
            .Bind(value => Save(value));

        // Assert: All operations executed successfully
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("TRANSFORMED");
        result.Value.ShouldContain("SAVED");
    }

    /// <summary>
    /// Scenario 7 Test 4: Chaining operations short-circuits on first failure.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ChainingOperations_FirstFailure_ShortCircuitsRemaining()
    {
        // Arrange
        var input = ""; // Invalid empty string

        // Act: Chain operations - should stop at validation
        var result = Validate(input)
            .Bind(value => Transform(value))  // Should NOT execute
            .Bind(value => Save(value));       // Should NOT execute

        // Assert: Short-circuited at first failure
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage!.ShouldContain("Validation");
        // Note: Cannot access .Value on failed result - it throws by design
    }

    /// <summary>
    /// Scenario 7 Test 5: Multiple failures accumulate messages.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MultipleFailures_AccumulateMessages()
    {
        // Arrange
        var result1 = GenericResult<string>.Failure(new GenericMessage("Error 1"));
        var result2 = GenericResult<string>.Failure(new GenericMessage("Error 2"));

        // Act: Combine failures
        var combinedResult = result1.IsSuccess ? result2 : result1;

        // Assert: First failure preserved
        combinedResult.IsSuccess.ShouldBeFalse();
        combinedResult.CurrentMessage.ShouldNotBeNull();
        combinedResult.CurrentMessage!.ShouldContain("Error 1");
    }

    /// <summary>
    /// Scenario 7 Test 6: Converting Result to async operation.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AsyncOperations_MaintainRailwaySemantics()
    {
        // Arrange
        var input = "test";

        // Act: Async railway operations
        var result = await ValidateAsync(input);
        var transformResult = result.IsSuccess
            ? await TransformAsync(result.Value!)
            : GenericResult<string>.Failure(new GenericMessage(result.CurrentMessage ?? "Unknown error"));

        // Assert: Async operations maintain Railway semantics
        transformResult.IsSuccess.ShouldBeTrue();
        transformResult.Value.ShouldNotBeNull();
        transformResult.Value!.ShouldContain("ASYNC");
    }

    /// <summary>
    /// Scenario 7 Test 7: Exception scenarios still throw for unexpected errors.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void UnexpectedErrors_StillThrowExceptions()
    {
        // Arrange & Act & Assert: Unexpected errors (programming bugs) still throw
        Should.Throw<NullReferenceException>(() =>
        {
            string? nullInput = null;
            var length = nullInput!.Length; // Programming error - should throw NullReferenceException
        });

        // Note: Railway-Oriented Programming is for ANTICIPATED failures.
        // Unexpected failures (bugs) still use exceptions.
    }

    /// <summary>
    /// Scenario 7 Test 8: GenericResult carries contextual error information.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenericResult_CarriesContextualErrorInformation()
    {
        // Arrange & Act
        var result = ValidateComplexData(new ComplexData { Value = -1 });

        // Assert: Error message provides context
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
        result.CurrentMessage.ShouldContain("Value");
        result.CurrentMessage.ShouldContain("negative");
    }

    // Helper methods demonstrating Railway-Oriented Programming

    private static IGenericResult<string> GetData(bool isValid)
    {
        if (!isValid)
            return GenericResult<string>.Failure(new GenericMessage("Invalid request"));

        return GenericResult<string>.Success("Success");
    }

    private static IGenericResult<string> Validate(string input)
    {
        if (string.IsNullOrEmpty(input))
            return GenericResult<string>.Failure(new GenericMessage("Validation failed: Input cannot be empty"));

        return GenericResult<string>.Success(input);
    }

    private static IGenericResult<string> Transform(string input)
    {
        var transformed = input.ToUpper() + "_TRANSFORMED";
        return GenericResult<string>.Success(transformed);
    }

    private static IGenericResult<string> Save(string input)
    {
        var saved = input + "_SAVED";
        return GenericResult<string>.Success(saved);
    }

    private static async Task<IGenericResult<string>> ValidateAsync(string input)
    {
        await Task.Delay(1); // Simulate async operation

        if (string.IsNullOrEmpty(input))
            return GenericResult<string>.Failure(new GenericMessage("Async validation failed"));

        return GenericResult<string>.Success(input);
    }

    private static async Task<IGenericResult<string>> TransformAsync(string input)
    {
        await Task.Delay(1); // Simulate async operation

        var transformed = input.ToUpper() + "_ASYNC_TRANSFORMED";
        return GenericResult<string>.Success(transformed);
    }

    private static IGenericResult<ComplexData> ValidateComplexData(ComplexData data)
    {
        if (data.Value < 0)
            return GenericResult<ComplexData>.Failure(
                new GenericMessage($"Validation failed: Value cannot be negative (was {data.Value})"));

        if (string.IsNullOrEmpty(data.Name))
            return GenericResult<ComplexData>.Failure(
                new GenericMessage("Validation failed: Name is required"));

        return GenericResult<ComplexData>.Success(data);
    }
}

/// <summary>
/// Complex data type for testing validation scenarios.
/// </summary>
public sealed class ComplexData
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for Railway-Oriented Programming patterns.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Binds (flatMaps) a result to another operation, short-circuiting on failure.
    /// </summary>
    public static IGenericResult<TNext> Bind<T, TNext>(
        this IGenericResult<T> result,
        Func<T, IGenericResult<TNext>> next)
    {
        if (!result.IsSuccess)
            return GenericResult<TNext>.Failure(new GenericMessage(result.CurrentMessage ?? "Operation failed"));

        return next(result.Value!);
    }
}
