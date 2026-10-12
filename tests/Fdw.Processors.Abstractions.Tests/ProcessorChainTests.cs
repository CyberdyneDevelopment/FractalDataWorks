using Fdw.Processors;
using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Processors.Tests;

/// <summary>
/// Tests for the <see cref="ProcessorChain{TCommand}"/> class.
/// </summary>
public class ProcessorChainTests
{
    #region Add Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Add_WithValidProcessor_AddsToChain()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext { Value = "test" };

        // Act
        var result = chain.Add(processor.Object, context);

        // Assert
        result.ShouldBe(chain); // Fluent interface
        chain.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Add_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var context = new TestContext();

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(
            () => chain.Add<TestContext>(null!, context));
        exception.ParamName.ShouldBe("processor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Add_MultipleProcessors_AddsAllToChain()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor1 = new Mock<IProcessor<string, TestContext>>();
        var processor2 = new Mock<IProcessor<string, TestContext>>();
        var processor3 = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        // Act
        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context)
             .Add(processor3.Object, context);

        // Assert
        chain.Count.ShouldBe(3);
    }

    #endregion

    #region AddIf Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddIf_WhenConditionTrue_AddsProcessor()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        // Act
        var result = chain.AddIf(true, processor.Object, context);

        // Assert
        result.ShouldBe(chain); // Fluent interface
        chain.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddIf_WhenConditionFalse_DoesNotAddProcessor()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        // Act
        var result = chain.AddIf(false, processor.Object, context);

        // Assert
        result.ShouldBe(chain); // Still fluent
        chain.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddIf_WhenConditionTrueAndNullProcessor_ThrowsArgumentNullException()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var context = new TestContext();

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(
            () => chain.AddIf<TestContext>(true, null!, context));
        exception.ParamName.ShouldBe("processor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddIf_WhenConditionFalseAndNullProcessor_DoesNotThrow()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var context = new TestContext();

        // Act
        var result = chain.AddIf<TestContext>(false, null!, context);

        // Assert
        result.ShouldBe(chain);
        chain.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddIf_MixedConditions_AddsOnlyWhenTrue()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor1 = new Mock<IProcessor<string, TestContext>>();
        var processor2 = new Mock<IProcessor<string, TestContext>>();
        var processor3 = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        // Act
        chain.AddIf(true, processor1.Object, context)
             .AddIf(false, processor2.Object, context)
             .AddIf(true, processor3.Object, context);

        // Assert
        chain.Count.ShouldBe(2); // Only processor1 and processor3
    }

    #endregion

    #region Execute Tests - Empty Chain

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithEmptyChain_ReturnsOriginalCommandAsSuccess()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        const string command = "original command";

        // Act
        var result = chain.Execute(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithEmptyChain_PreservesCommandValue()
    {
        // Arrange
        var chain = new ProcessorChain<int>();
        const int command = 42;

        // Act
        var result = chain.Execute(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(command);
    }

    #endregion

    #region Execute Tests - Single Processor

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithSingleSuccessfulProcessor_ReturnsProcessedCommand()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();
        const string input = "hello";
        const string output = "HELLO";

        processor.Setup(p => p.Process(input, context))
                 .Returns(GenericResult<string>.Success(output));

        chain.Add(processor.Object, context);

        // Act
        var result = chain.Execute(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(output);
        processor.Verify(p => p.Process(input, context), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithSingleFailedProcessor_ReturnsFailure()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();
        const string input = "hello";
        const string errorMessage = "Processing failed";

        processor.Setup(p => p.Process(input, context))
                 .Returns(GenericResult<string>.Failure(new GenericMessage(errorMessage)));

        chain.Add(processor.Object, context);

        // Act
        var result = chain.Execute(input);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe(errorMessage);
        processor.Verify(p => p.Process(input, context), Times.Once);
    }

    #endregion

    #region Execute Tests - Multiple Processors

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithMultipleSuccessfulProcessors_ChainsCorrectly()
    {
        // Arrange
        var chain = new ProcessorChain<int>();
        var processor1 = new Mock<IProcessor<int, TestContext>>();
        var processor2 = new Mock<IProcessor<int, TestContext>>();
        var processor3 = new Mock<IProcessor<int, TestContext>>();
        var context = new TestContext();

        // Setup processors to transform: 10 -> 20 -> 30 -> 40
        processor1.Setup(p => p.Process(10, context))
                  .Returns(GenericResult<int>.Success(20));
        processor2.Setup(p => p.Process(20, context))
                  .Returns(GenericResult<int>.Success(30));
        processor3.Setup(p => p.Process(30, context))
                  .Returns(GenericResult<int>.Success(40));

        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context)
             .Add(processor3.Object, context);

        // Act
        var result = chain.Execute(10);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(40);
        processor1.Verify(p => p.Process(10, context), Times.Once);
        processor2.Verify(p => p.Process(20, context), Times.Once);
        processor3.Verify(p => p.Process(30, context), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithMultipleProcessors_PassesOutputToNextInput()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor1 = new Mock<IProcessor<string, TestContext>>();
        var processor2 = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        processor1.Setup(p => p.Process("input", context))
                  .Returns(GenericResult<string>.Success("intermediate"));
        processor2.Setup(p => p.Process("intermediate", context))
                  .Returns(GenericResult<string>.Success("final"));

        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context);

        // Act
        var result = chain.Execute("input");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("final");
    }

    #endregion

    #region Execute Tests - Railway Pattern (Short-Circuit on Failure)

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WhenFirstProcessorFails_StopsImmediately()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor1 = new Mock<IProcessor<string, TestContext>>();
        var processor2 = new Mock<IProcessor<string, TestContext>>();
        var processor3 = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();
        const string errorMessage = "First processor failed";

        processor1.Setup(p => p.Process("input", context))
                  .Returns(GenericResult<string>.Failure(new GenericMessage(errorMessage)));

        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context)
             .Add(processor3.Object, context);

        // Act
        var result = chain.Execute("input");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe(errorMessage);
        processor1.Verify(p => p.Process(It.IsAny<string>(), context), Times.Once);
        processor2.Verify(p => p.Process(It.IsAny<string>(), context), Times.Never);
        processor3.Verify(p => p.Process(It.IsAny<string>(), context), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WhenMiddleProcessorFails_StopsAtFailure()
    {
        // Arrange
        var chain = new ProcessorChain<int>();
        var processor1 = new Mock<IProcessor<int, TestContext>>();
        var processor2 = new Mock<IProcessor<int, TestContext>>();
        var processor3 = new Mock<IProcessor<int, TestContext>>();
        var context = new TestContext();
        const string errorMessage = "Middle processor failed";

        processor1.Setup(p => p.Process(10, context))
                  .Returns(GenericResult<int>.Success(20));
        processor2.Setup(p => p.Process(20, context))
                  .Returns(GenericResult<int>.Failure(new GenericMessage(errorMessage)));

        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context)
             .Add(processor3.Object, context);

        // Act
        var result = chain.Execute(10);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe(errorMessage);
        processor1.Verify(p => p.Process(10, context), Times.Once);
        processor2.Verify(p => p.Process(20, context), Times.Once);
        processor3.Verify(p => p.Process(It.IsAny<int>(), context), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WhenLastProcessorFails_ReturnsFailure()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor1 = new Mock<IProcessor<string, TestContext>>();
        var processor2 = new Mock<IProcessor<string, TestContext>>();
        var processor3 = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();
        const string errorMessage = "Last processor failed";

        processor1.Setup(p => p.Process("a", context))
                  .Returns(GenericResult<string>.Success("b"));
        processor2.Setup(p => p.Process("b", context))
                  .Returns(GenericResult<string>.Success("c"));
        processor3.Setup(p => p.Process("c", context))
                  .Returns(GenericResult<string>.Failure(new GenericMessage(errorMessage)));

        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context)
             .Add(processor3.Object, context);

        // Act
        var result = chain.Execute("a");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe(errorMessage);
        processor1.Verify(p => p.Process("a", context), Times.Once);
        processor2.Verify(p => p.Process("b", context), Times.Once);
        processor3.Verify(p => p.Process("c", context), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WhenProcessorReturnsNullValue_StopsImmediately()
    {
        // Arrange
        var chain = new ProcessorChain<string?>();
        var processor1 = new Mock<IProcessor<string?, TestContext>>();
        var processor2 = new Mock<IProcessor<string?, TestContext>>();
        var context = new TestContext();

        // Processor1 returns success but with null value
        processor1.Setup(p => p.Process("input", context))
                  .Returns(GenericResult<string?>.Success(null));

        chain.Add(processor1.Object, context)
             .Add(processor2.Object, context);

        // Act
        var result = chain.Execute("input");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
        processor1.Verify(p => p.Process("input", context), Times.Once);
        processor2.Verify(p => p.Process(It.IsAny<string?>(), context), Times.Never);
    }

    #endregion

    #region Execute Tests - Different Context Types

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_WithDifferentContextTypes_WorksCorrectly()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor1 = new Mock<IProcessor<string, TestContext>>();
        var processor2 = new Mock<IProcessor<string, OtherContext>>();
        var context1 = new TestContext { Value = "ctx1" };
        var context2 = new OtherContext { Name = "ctx2" };

        processor1.Setup(p => p.Process("input", context1))
                  .Returns(GenericResult<string>.Success("step1"));
        processor2.Setup(p => p.Process("step1", context2))
                  .Returns(GenericResult<string>.Success("final"));

        chain.Add(processor1.Object, context1)
             .Add(processor2.Object, context2);

        // Act
        var result = chain.Execute("input");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("final");
        processor1.Verify(p => p.Process("input", context1), Times.Once);
        processor2.Verify(p => p.Process("step1", context2), Times.Once);
    }

    #endregion

    #region Count Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Count_WithEmptyChain_ReturnsZero()
    {
        // Arrange
        var chain = new ProcessorChain<string>();

        // Act & Assert
        chain.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Count_AfterAddingProcessors_ReturnsCorrectCount()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        // Act & Assert
        chain.Count.ShouldBe(0);
        chain.Add(processor.Object, context);
        chain.Count.ShouldBe(1);
        chain.Add(processor.Object, context);
        chain.Count.ShouldBe(2);
        chain.Add(processor.Object, context);
        chain.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Count_WithConditionalAdds_ReturnsCorrectCount()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var processor = new Mock<IProcessor<string, TestContext>>();
        var context = new TestContext();

        // Act
        chain.AddIf(true, processor.Object, context)
             .AddIf(false, processor.Object, context)
             .AddIf(true, processor.Object, context)
             .AddIf(false, processor.Object, context);

        // Assert
        chain.Count.ShouldBe(2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_RealWorldScenario_SignEncryptTransformPipeline()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var signingProcessor = new TestSigningProcessor();
        var encryptionProcessor = new TestEncryptionProcessor();
        var transformProcessor = new TestTransformProcessor();
        var config = new PipelineConfig
        {
            EnableEncryption = true,
            EnableSigning = true,
            EnableTransform = true
        };

        // Act
        chain.AddIf(config.EnableSigning, signingProcessor, new TestContext())
             .AddIf(config.EnableEncryption, encryptionProcessor, new TestContext())
             .AddIf(config.EnableTransform, transformProcessor, new TestContext());

        var result = chain.Execute("data");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("data-signed-encrypted-transformed");
        chain.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Execute_RealWorldScenario_WithEncryptionDisabled()
    {
        // Arrange
        var chain = new ProcessorChain<string>();
        var signingProcessor = new TestSigningProcessor();
        var encryptionProcessor = new TestEncryptionProcessor();
        var transformProcessor = new TestTransformProcessor();
        var config = new PipelineConfig
        {
            EnableEncryption = false, // Disabled
            EnableSigning = true,
            EnableTransform = true
        };

        // Act
        chain.AddIf(config.EnableSigning, signingProcessor, new TestContext())
             .AddIf(config.EnableEncryption, encryptionProcessor, new TestContext())
             .AddIf(config.EnableTransform, transformProcessor, new TestContext());

        var result = chain.Execute("data");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("data-signed-transformed"); // No encryption step
        chain.Count.ShouldBe(2);
    }

    #endregion

    #region Test Helper Classes

    [ExcludeFromCodeCoverage]
    public class TestContext
    {
        public string Value { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class OtherContext
    {
        public string Name { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class PipelineConfig
    {
        public bool EnableSigning { get; set; }
        public bool EnableEncryption { get; set; }
        public bool EnableTransform { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class TestSigningProcessor : IProcessor<string, TestContext>
    {
        public bool IsEmpty => false;
        public IReadOnlyList<string> RequiredProperties => Array.Empty<string>();

        public IGenericResult Validate(TestContext context) => GenericResult.Success();

        public IGenericResult<string> Process(string command, TestContext context)
        {
            return GenericResult<string>.Success(command + "-signed");
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestEncryptionProcessor : IProcessor<string, TestContext>
    {
        public bool IsEmpty => false;
        public IReadOnlyList<string> RequiredProperties => Array.Empty<string>();

        public IGenericResult Validate(TestContext context) => GenericResult.Success();

        public IGenericResult<string> Process(string command, TestContext context)
        {
            return GenericResult<string>.Success(command + "-encrypted");
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestTransformProcessor : IProcessor<string, TestContext>
    {
        public bool IsEmpty => false;
        public IReadOnlyList<string> RequiredProperties => Array.Empty<string>();

        public IGenericResult Validate(TestContext context) => GenericResult.Success();

        public IGenericResult<string> Process(string command, TestContext context)
        {
            return GenericResult<string>.Success(command + "-transformed");
        }
    }

    #endregion
}
