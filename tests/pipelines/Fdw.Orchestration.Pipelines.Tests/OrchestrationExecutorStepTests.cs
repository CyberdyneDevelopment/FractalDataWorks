using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.Resilience;
using Fdw.Orchestration.Execution;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Tests;

public class OrchestrationExecutorStepTests
{
    private sealed class ProbeExecutor : OrchestrationExecutor
    {
        public ProbeExecutor(IServiceProvider serviceProvider)
            : base(new Mock<IResiliencePipelineFactory>().Object, serviceProvider)
        {
        }

        public Task<object?> Run(OrchestrationStepExecutionContext context, CancellationToken cancellationToken = default)
        {
            return ExecuteStepCore(context.Step, context, cancellationToken);
        }
    }

    private static IServiceProvider ServicesHolding(IOrchestrationStepExecutor? stepExecutor)
    {
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IOrchestrationStepExecutor))).Returns(stepExecutor);
        return services.Object;
    }

    private static OrchestrationStepExecutionContext StepContext(IServiceProvider services)
    {
        var step = new Mock<IOrchestrationStep>();
        step.SetupGet(s => s.StepId).Returns("load-customers");
        step.SetupGet(s => s.DependsOn).Returns(Array.Empty<string>());

        var orchestration = new Mock<IOrchestrationContext>();
        orchestration.SetupGet(c => c.ExecutionId).Returns(Guid.NewGuid());
        orchestration.SetupGet(c => c.Services).Returns(services);
        orchestration.SetupGet(c => c.Parameters).Returns(new Dictionary<string, object?>());
        orchestration.SetupGet(c => c.SharedState).Returns(new Dictionary<string, object?>());

        return new OrchestrationStepExecutionContext("step-run-1", step.Object, orchestration.Object);
    }

    [Fact]
    public async Task ExecuteStepCore_NoStepExecutorRegistered_ThrowsWithTheCodedReason()
    {
        var services = ServicesHolding(null);
        var probe = new ProbeExecutor(services);

        var failure = await Should.ThrowAsync<InvalidOperationException>(() => probe.Run(StepContext(services)));

        failure.Message.ShouldContain("load-customers");
        failure.Message.ShouldContain("was not run");
    }

    [Fact]
    public async Task ExecuteStepCore_StepExecutorRegistered_ReturnsItsOutput()
    {
        var output = new Mock<IOrchestrationStepResult>();
        output.SetupGet(r => r.Output).Returns("42 rows");

        var stepExecutor = new Mock<IOrchestrationStepExecutor>();
        stepExecutor
            .Setup(e => e.Execute(It.IsAny<IOrchestrationStep>(), It.IsAny<IOrchestrationContext>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IOrchestrationStepResult>.Success(output.Object));

        var services = ServicesHolding(stepExecutor.Object);
        var probe = new ProbeExecutor(services);

        var result = await probe.Run(StepContext(services));

        result.ShouldBe("42 rows");
    }
}
