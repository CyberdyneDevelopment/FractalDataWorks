using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// The row provider the test double's pipeline types register on the pipe.Pipeline domain provider under their names (FDW-6244), as a graph pipeline type
/// registers its graph row provider. It is never read; the gateway provider it is given is the test host's.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EchoRowProvider : ImplementationProviderBase<EchoPipelineRow, IPipelineImplementationConfiguration>
{
    /// <summary>The schema the test rows would live in.</summary>
    public const string RowPath = "pipe";

    /// <summary>The container the test rows would live in.</summary>
    public const string RowContainer = "EchoPipelineRow";

    /// <summary>Initializes a new instance of the <see cref="EchoRowProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">The test host's gateway provider; never called.</param>
    public EchoRowProvider(ILogger<EchoRowProvider>? logger, IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, PipelineTypes.ConfigurationConnection, RowPath, RowContainer)
    {
    }
}
