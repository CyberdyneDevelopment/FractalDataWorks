using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// One host started through the Pipeline domain (FDW-6244), shared by a test class: <see cref="PipelineTypes"/> configured, registered and initialized
/// with every option this assembly collects (GraphPipelineType, and the test double's Echo, Unrouted and Silent). A pipeline type's phases run once, so
/// the host is built once and every test reads it.
/// </summary>
/// <remarks>
/// The host names no operational store, so it runs no pipelines and is not asked for runners; the gateway provider and threshold evaluator are mocks
/// nothing calls.
/// </remarks>
public sealed class StartedPipelineDomain : IDisposable
{
    private const bool Force = true;

    private readonly string? _before = PipelineTypes.OperationalConnection;

    /// <summary>Initializes a new instance of the <see cref="StartedPipelineDomain"/> class and starts the host.</summary>
    public StartedPipelineDomain()
    {
        PipelineTypes.OperationalConnection = null;
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddLogging();
        builder.Services.AddSingleton(new Mock<Fdw.Services.Descriptions.IDescriptionWriter>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>(MockBehavior.Strict).Object);
        PlatformHostServices.Add(builder.Services);
        builder.Services.AddSingleton(new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IThresholdEvaluator>(MockBehavior.Strict).Object);

        // Why each option is registered here, forced, before the domain registers (FDW-6270): an option's phases run once per process, so this host
        // gets every option's registrations whatever ran before it in this process. The domain's own Register then skips them.
        foreach (var type in PipelineTypes.All().Values)
        {
            type.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue(type.Name);
        }

        Configured = PipelineTypes.Configure(builder, NullLoggerFactory.Instance, Force);
        Registered = PipelineTypes.Register(builder, NullLoggerFactory.Instance, Force);
        BuiltHost = builder.Build();
        Initialized = PipelineTypes.Initialize(BuiltHost, NullLoggerFactory.Instance, Force);
    }

    /// <summary>Gets the result of the domain's Configure phase.</summary>
    public IGenericResult Configured { get; }

    /// <summary>Gets the result of the domain's Register phase.</summary>
    public IGenericResult Registered { get; }

    /// <summary>Gets the built host.</summary>
    public IHost BuiltHost { get; }

    /// <summary>Gets the result of the domain's Initialize phase.</summary>
    public IGenericResult<IHost> Initialized { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        BuiltHost.Dispose();
        PipelineTypes.OperationalConnection = _before;
    }
}
