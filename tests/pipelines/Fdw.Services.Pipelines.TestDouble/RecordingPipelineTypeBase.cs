using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// An abstract generic base between <see cref="PipelineTypeBase{TService,TFactory,TConfiguration}"/> and a closed option, as
/// <c>GraphPipelineTypeBase&lt;TIn, TOut&gt;</c> is between it and <c>GraphPipelineType</c> (FDW-6244). It holds a static Registration body that each closing sets in its own
/// constructor, and counts the runs of it, so a test proves an option it closes is collected, found by name, and registered once.
/// </summary>
/// <typeparam name="TIn">What a pipeline of this type takes.</typeparam>
/// <typeparam name="TOut">What a pipeline of this type answers with.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class RecordingPipelineTypeBase<TIn, TOut>
    : PipelineTypeBase<IPipeline<TIn, TOut>, IServiceFactory<IPipeline<TIn, TOut>, EchoPipelineConfiguration>, EchoPipelineConfiguration>
{
    private int _registrationRuns;

    /// <summary>Initializes a new instance of the <see cref="RecordingPipelineTypeBase{TIn, TOut}"/> class.</summary>
    /// <param name="name">The option's name, as a pipeline's domain row names its implementation.</param>
    /// <param name="runModeName">The run mode a run of this type is carried out in.</param>
    protected RecordingPipelineTypeBase(string name, string runModeName)
        : base(name, name, name, "A test pipeline type over an abstract generic base", runModeName)
    {
    }

    /// <summary>The Register body each closing sets in its own constructor (STC002: only the declared service type sets a phase func); it counts its runs.</summary>
    /// <param name="type">The option whose Registration ran.</param>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder.</returns>
    protected static IGenericResult<IHostApplicationBuilder> Record(RecordingPipelineTypeBase<TIn, TOut> type, IHostApplicationBuilder builder)
    {
        _ = Interlocked.Increment(ref type._registrationRuns);
        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// The Initialize body each closing sets in its own constructor: registers an <see cref="EchoRowProvider"/> on the pipe.Pipeline domain provider under
    /// the option's name, as every pipeline type registers its row provider (FDW-6244).
    /// </summary>
    /// <param name="type">The option being initialized.</param>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one.</param>
    /// <returns>The host, or the domain provider's refusal.</returns>
    protected static IGenericResult<IHost> RegisterRow(RecordingPipelineTypeBase<TIn, TOut> type, IHost host, ILoggerFactory? loggerFactory)
    {
        var registered = host.Services.GetRequiredService<IPipelineConfigurationProvider>().Register(
            type.Name,
            new EchoRowProvider(
                (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<EchoRowProvider>(),
                host.Services.GetRequiredService<IConfigurationGatewayProvider>()));
        return registered.IsSuccess ? GenericResult<IHost>.Success(host) : registered.ToNewResult<IHost>();
    }

    /// <summary>Gets how many times this option's Registration body ran.</summary>
    public int RegistrationRuns => Volatile.Read(ref _registrationRuns);
}
