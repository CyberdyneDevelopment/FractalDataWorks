using Fdw.Configuration;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.Tests;

public class StageTypeBaseTests
{
    private sealed class TestStageType : StageTypeBase
    {
        public TestStageType(
            int id,
            string name,
            bool requiresSource,
            bool requiresDestination,
            bool supportsParallel,
            bool producesOutput = true,
            bool consumesInput = true)
            : base(id, name, requiresSource, requiresDestination, supportsParallel, producesOutput, consumesInput)
        {
        }

        public override Task<IGenericResult> ValidateConfiguration(
            IGenericConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsIdAndName()
    {
        var sut = new TestStageType(1, "Extract",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: true);

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("Extract");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsRequiresSource()
    {
        var sut = new TestStageType(1, "Extract",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: false);

        sut.RequiresSource.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsRequiresDestination()
    {
        var sut = new TestStageType(3, "Load",
            requiresSource: false,
            requiresDestination: true,
            supportsParallel: false);

        sut.RequiresDestination.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsSupportsParallel()
    {
        var sut = new TestStageType(2, "Transform",
            requiresSource: false,
            requiresDestination: false,
            supportsParallel: true);

        sut.SupportsParallel.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ProducesOutputDefaultsToTrue()
    {
        var sut = new TestStageType(1, "Extract",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: false);

        sut.ProducesOutput.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConsumesInputDefaultsToTrue()
    {
        var sut = new TestStageType(1, "Extract",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: false);

        sut.ConsumesInput.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ProducesOutputCanBeSetToFalse()
    {
        var sut = new TestStageType(4, "Sink",
            requiresSource: false,
            requiresDestination: true,
            supportsParallel: false,
            producesOutput: false);

        sut.ProducesOutput.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConsumesInputCanBeSetToFalse()
    {
        var sut = new TestStageType(1, "Source",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: false,
            consumesInput: false);

        sut.ConsumesInput.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateConfigurationCanBeInvoked()
    {
        var sut = new TestStageType(1, "Test",
            requiresSource: false,
            requiresDestination: false,
            supportsParallel: false);

        var config = new Mock<IGenericConfiguration>();
        var result = await sut.ValidateConfiguration(config.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExtractStageTypicalConfiguration()
    {
        var sut = new TestStageType(1, "Extract",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: true,
            producesOutput: true,
            consumesInput: false);

        sut.RequiresSource.ShouldBeTrue();
        sut.RequiresDestination.ShouldBeFalse();
        sut.SupportsParallel.ShouldBeTrue();
        sut.ProducesOutput.ShouldBeTrue();
        sut.ConsumesInput.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void LoadStageTypicalConfiguration()
    {
        var sut = new TestStageType(3, "Load",
            requiresSource: false,
            requiresDestination: true,
            supportsParallel: false,
            producesOutput: false,
            consumesInput: true);

        sut.RequiresSource.ShouldBeFalse();
        sut.RequiresDestination.ShouldBeTrue();
        sut.SupportsParallel.ShouldBeFalse();
        sut.ProducesOutput.ShouldBeFalse();
        sut.ConsumesInput.ShouldBeTrue();
    }
}
