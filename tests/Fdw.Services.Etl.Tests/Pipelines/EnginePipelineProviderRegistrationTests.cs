using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// The Pipeline domain provider only accepts an implementation provider typed on ITS contract, and
/// <c>IImplementationConfigurationProvider&lt;T&gt;</c> is invariant. Both engine providers were typed on the narrower
/// Etl contract, so registration was refused at host start and no engine was ever registered -- every
/// pipeline create then failed with "No implementation configuration provider registered".
/// </summary>
public sealed class EnginePipelineProviderRegistrationTests
{
    private static PipelineServiceConfigurationProvider CreateDomainProvider() =>
        new(NullLogger<PipelineServiceConfigurationProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(),
            "ConfigurationDb", "pipe", "Pipeline");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BatchCopyProviderRegistersIntoThePipelineDomainProvider()
    {
        var domain = CreateDomainProvider();
        var engine = new BatchCopyPipelineConfigurationProvider(
            NullLogger<BatchCopyPipelineConfigurationProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(),
            "ConfigurationDb", "pipe", "BatchCopyPipelineConfiguration");

        domain.Register("BatchCopy", engine).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void StreamingProviderRegistersIntoThePipelineDomainProvider()
    {
        var domain = CreateDomainProvider();
        var engine = new StreamingPipelineConfigurationProvider(
            NullLogger<StreamingPipelineConfigurationProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(),
            "ConfigurationDb", "pipe", "StreamingPipelineConfiguration");

        domain.Register("Streaming", engine).IsSuccess.ShouldBeTrue();
    }
}
