using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6244: <see cref="GraphPipelineTypeBase{TIn, TOut}"/> is proven with two closings in one host before a request pipeline depends on it. The ETL closing,
/// <see cref="GraphPipelineType"/>, and a second one, <see cref="RequestGraphPipelineType"/>, each register and initialize through the base's bodies; both read
/// their rows from the one graph row container, and each registers that row's provider on the Pipeline domain provider under its own name.
/// </summary>
/// <remarks>
/// The host is one that does not run pipelines (FDW-6528): a host that names its operational store is asked, at Initialize, for the services that judge the person a run
/// acts as, and this test is about the base's own bodies. <c>PipelineTypes.OperationalConnection</c> is static, so the class is serialized with the others that set it and
/// states the value it needs, as <c>StartedPipelineDomain</c> does.
/// </remarks>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class GraphPipelineTypeBaseHostTests : IDisposable
{
    private readonly string? _before = PipelineTypes.OperationalConnection;

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TwoClosingsOfTheGenericGraphBaseRegisterAndInitializeInOneHost()
    {
        PipelineTypes.OperationalConnection = null;
        var graph = new GraphPipelineType();
        var request = new RequestGraphPipelineType();
        var domain = new Mock<IPipelineConfigurationProvider>(MockBehavior.Strict);
        domain.Setup(provider => provider.Register(GraphPipelineType.OptionName, It.IsAny<GraphPipelineConfigurationProvider>())).Returns(GenericResult.Success());
        domain.Setup(provider => provider.Register(RequestGraphPipelineType.OptionName, It.IsAny<GraphPipelineConfigurationProvider>())).Returns(GenericResult.Success());
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddLogging();
        PlatformHostServices.Add(builder.Services);
        builder.Services.AddSingleton(domain.Object);
        builder.Services.AddSingleton(new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<Fdw.Services.Descriptions.IDescriptionWriter>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IThresholdEvaluator>(MockBehavior.Strict).Object);

        graph.Configure(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        request.Configure(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        var graphRegistered = graph.Register(builder, NullLoggerFactory.Instance);
        graphRegistered.IsSuccess.ShouldBeTrue(graphRegistered.CurrentMessage);
        var requestRegistered = request.Register(builder, NullLoggerFactory.Instance);
        requestRegistered.IsSuccess.ShouldBeTrue(requestRegistered.CurrentMessage);
        using var host = builder.Build();

        var graphInitialized = graph.Initialize(host, NullLoggerFactory.Instance);
        graphInitialized.IsSuccess.ShouldBeTrue(graphInitialized.CurrentMessage);
        var requestInitialized = request.Initialize(host, NullLoggerFactory.Instance);
        requestInitialized.IsSuccess.ShouldBeTrue(requestInitialized.CurrentMessage);

        request.Container.ShouldBe(graph.Container);
        domain.Verify(provider => provider.Register(GraphPipelineType.OptionName, It.IsAny<GraphPipelineConfigurationProvider>()), Times.Once);
        domain.Verify(provider => provider.Register(RequestGraphPipelineType.OptionName, It.IsAny<GraphPipelineConfigurationProvider>()), Times.Once);
    }
}
