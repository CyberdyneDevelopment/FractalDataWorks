using System.Threading;
using System.Threading.Tasks;
using Fdw.Processors;
using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Processors.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="AsyncProcessorBase{TCommand, TContext, TBase}"/> class.
/// </summary>
public class AsyncProcessorBaseTests
{
    #region Test Types

    private sealed record TestContext(string? Username, string? Password);

    private sealed class TestAsyncProcessor : AsyncProcessorBase<string, TestContext, TestAsyncProcessor>
    {
        public TestAsyncProcessor()
            : base("AsyncTest", "Async Test Processor", "An async test processor", new[] { "Username", "Password" })
        {
        }

        public override async Task<IGenericResult<string>> Process(
            string command,
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            var validationResult = Validate(context);
            if (!validationResult.IsSuccess)
            {
                return GenericResult<string>.Failure(new GenericMessage(validationResult.CurrentMessage ?? "Validation failed"));
            }

            await Task.Delay(10, cancellationToken);
            return GenericResult<string>.Success($"{command}:{context.Username}");
        }
    }

    private sealed class NoRequiredPropertiesAsyncProcessor : AsyncProcessorBase<string, TestContext, NoRequiredPropertiesAsyncProcessor>
    {
        public NoRequiredPropertiesAsyncProcessor()
            : base("NoReqsAsync", "No Requirements Async", "No required properties", Array.Empty<string>())
        {
        }

        public override Task<IGenericResult<string>> Process(
            string command,
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GenericResult<string>.Success(command));
        }
    }

    private sealed class EmptyAsyncProcessor : AsyncProcessorBase<string, TestContext, EmptyAsyncProcessor>
    {
        public EmptyAsyncProcessor() : base()
        {
        }

        public override Task<IGenericResult<string>> Process(
            string command,
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GenericResult<string>.Failure(new GenericMessage("Empty processor cannot process")));
        }
    }

    private sealed class CancellableProcessor : AsyncProcessorBase<string, TestContext, CancellableProcessor>
    {
        public CancellableProcessor()
            : base("Cancellable", "Cancellable Processor", "Respects cancellation", Array.Empty<string>())
        {
        }

        public override async Task<IGenericResult<string>> Process(
            string command,
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(5000, cancellationToken);
            return GenericResult<string>.Success(command);
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var processor = new TestAsyncProcessor();

        // Assert
        processor.Name.ShouldBe("AsyncTest");
        processor.DisplayName.ShouldBe("Async Test Processor");
        processor.Description.ShouldBe("An async test processor");
        processor.Category.ShouldBe("Processor");
        processor.RequiredProperties.Count.ShouldBe(2);
        processor.RequiredProperties[0].ShouldBe("Username");
        processor.RequiredProperties[1].ShouldBe("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EmptyConstructor_CreatesEmptyProcessor()
    {
        // Arrange & Act
        var processor = new EmptyAsyncProcessor();

        // Assert
        processor.Name.ShouldBe(string.Empty);
        processor.DisplayName.ShouldBe(string.Empty);
        processor.Description.ShouldBe(string.Empty);
        processor.IsEmpty.ShouldBeTrue();
        processor.RequiredProperties.ShouldBeEmpty();
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsEmpty_ForNormalProcessor_ReturnsFalse()
    {
        // Arrange
        var processor = new TestAsyncProcessor();

        // Act
        var result = processor.IsEmpty;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsEmpty_ForEmptyProcessor_ReturnsTrue()
    {
        // Arrange
        var processor = new EmptyAsyncProcessor();

        // Act
        var result = processor.IsEmpty;

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Validate Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Validate_WithValidContext_ReturnsSuccess()
    {
        // Arrange
        var processor = new TestAsyncProcessor();
        var context = new TestContext("user", "pass");

        // Act
        var result = processor.Validate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Validate_ForNoRequiredProperties_ReturnsSuccess()
    {
        // Arrange
        var processor = new NoRequiredPropertiesAsyncProcessor();
        var context = new TestContext(null, null);

        // Act
        var result = processor.Validate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Process Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task Process_WithValidContext_ReturnsProcessedCommand()
    {
        // Arrange
        var processor = new TestAsyncProcessor();
        var command = "test-command";
        var context = new TestContext("testuser", "testpass");

        // Act
#pragma warning disable xUnit1051 // Test uses default CancellationToken
        var result = await processor.Process(command, context);
#pragma warning restore xUnit1051

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test-command:testuser");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task Process_WhenCancelled_ThrowsOperationCancelledException()
    {
        // Arrange
        var processor = new CancellableProcessor();
        var command = "test-command";
        var context = new TestContext(null, null);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await processor.Process(command, context, cts.Token));
    }

    #endregion

    #region GenerateIdFromName Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenerateIdFromName_WithSameName_GeneratesSameId()
    {
        // Arrange
        var processor1 = new TestAsyncProcessor();
        var processor2 = new TestAsyncProcessor();

        // Act & Assert
        processor1.Id.ShouldBe(processor2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenerateIdFromName_WithDifferentNames_GeneratesDifferentIds()
    {
        // Arrange
        var processor1 = new TestAsyncProcessor();
        var processor2 = new NoRequiredPropertiesAsyncProcessor();

        // Act & Assert
        processor1.Id.ShouldNotBe(processor2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenerateIdFromName_ReturnsPositiveId()
    {
        // Arrange
        var processor = new TestAsyncProcessor();

        // Act & Assert
        processor.Id.ShouldBeGreaterThan(0);
    }

    #endregion

    #region RequiredProperties Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredProperties_ReturnsReadOnlyList()
    {
        // Arrange
        var processor = new TestAsyncProcessor();

        // Act
        var properties = processor.RequiredProperties;

        // Assert
        properties.ShouldNotBeNull();
        properties.ShouldBeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredProperties_ForEmptyProcessor_ReturnsEmptyList()
    {
        // Arrange
        var processor = new EmptyAsyncProcessor();

        // Act
        var properties = processor.RequiredProperties;

        // Assert
        properties.ShouldBeEmpty();
    }

    #endregion
}
