using Fdw.Results;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for IEtlPipeline interface contract.
/// </summary>
public class IEtlPipelineTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IdPropertyCanBeReadFromImplementation()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var pipeline = new TestEtlPipeline { Id = expectedId };

        // Act
        var result = pipeline.Id;

        // Assert
        result.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NamePropertyCanBeReadFromImplementation()
    {
        // Arrange
        const string expectedName = "TestPipeline";
        var pipeline = new TestEtlPipeline { Name = expectedName };

        // Act
        var result = pipeline.Name;

        // Assert
        result.ShouldBe(expectedName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PipelineTypePropertyCanBeReadFromImplementation()
    {
        // Arrange
        const string expectedType = "BatchCopy";
        var pipeline = new TestEtlPipeline { PipelineType = expectedType };

        // Act
        var result = pipeline.PipelineType;

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IsExecutingPropertyCanBeReadFromImplementation()
    {
        // Arrange
        var pipeline = new TestEtlPipeline { IsExecuting = true };

        // Act
        var result = pipeline.IsExecuting;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ExecuteCanBeCalledWithCancellationToken()
    {
        // Arrange
        var pipeline = new TestEtlPipeline();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await pipeline.Execute(cts.Token);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ExecuteCanBeCalledWithoutCancellationToken()
    {
        // Arrange
        var pipeline = new TestEtlPipeline();

        // Act
        var result = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateCanBeCalled()
    {
        // Arrange
        var pipeline = new TestEtlPipeline();

        // Act
        var result = pipeline.Validate();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ImplementsIDisposable()
    {
        // Arrange
        var pipeline = new TestEtlPipeline();

        // Act
        pipeline.Dispose();

        // Assert
        pipeline.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// Test implementation of IEtlPipeline.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestEtlPipeline : IEtlPipeline
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Test";
        public string PipelineType { get; set; } = "Test";
        public bool IsExecuting { get; set; }
        public bool IsDisposed { get; private set; }

        public Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(CancellationToken cancellationToken = default)
        {
            var result = Mock.Of<IEtlPipelineExecutionResult>();
            return Task.FromResult(GenericResult<IEtlPipelineExecutionResult>.Success(result));
        }

        public Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(
            Fdw.Services.Etl.Abstractions.Execution.PipelineExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = Mock.Of<IEtlPipelineExecutionResult>();
            return Task.FromResult(GenericResult<IEtlPipelineExecutionResult>.Success(result));
        }

        public IGenericResult Validate()
        {
            return GenericResult.Success();
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        // IGenericService members
        string IGenericService.Id => Id.ToString();
        string IGenericService.ServiceType => PipelineType;
        bool IGenericService.IsAvailable => !IsExecuting;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
