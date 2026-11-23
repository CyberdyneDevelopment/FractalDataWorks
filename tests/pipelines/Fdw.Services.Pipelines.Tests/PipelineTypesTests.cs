using System.Linq;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.TestDouble;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-6244: pipelines are one domain, <see cref="PipelineTypes"/>. An option that closes an abstract generic base (as GraphPipelineType closes
/// GraphPipelineTypeBase) and declares <c>[Implementation(typeof(PipelineTypes), ...)]</c> in another assembly is collected, found by the name a pipeline's
/// domain row gives as its implementation, and registered once; the domain itself registers the pipe.Pipeline configuration provider.
/// </summary>
public sealed class PipelineTypesTests
{
    private const bool Force = true;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AnOptionThatClosesAnAbstractGenericBaseIsFoundByItsImplementationName()
    {
        var found = PipelineTypes.ByName(EchoPipelineType.OptionName);

        found.ShouldBeOfType<EchoPipelineType>();
        found.RunModeName.ShouldBe(EchoPipelineType.RunMode);
        PipelineTypes.All().Values.Select(type => type.Name).ShouldContain(EchoPipelineType.OptionName);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheOptionsRegistrationRunsOnceHoweverOftenTheDomainRegisters()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        var echo = PipelineTypes.ByName(EchoPipelineType.OptionName).ShouldBeOfType<EchoPipelineType>();

        PipelineTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        PipelineTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        var afterFirst = echo.RegistrationRuns;
        PipelineTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();

        // Read against the first Register, not a fixed count: other tests in this process force Echo's Register for hosts of their own (FDW-6270).
        afterFirst.ShouldBeGreaterThan(0);
        echo.RegistrationRuns.ShouldBe(afterFirst);
        builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IPipelineConfigurationProvider)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnImplementationNoOptionNamesIsNotFoundAndItsRunModeIsNoneOfTheRunModes()
    {
        var notFound = PipelineTypes.ByName("Telepathic");

        notFound.ShouldBeSameAs(PipelineTypes.NotFound);
        PipelineRunModes.ByName(notFound.RunModeName).ShouldBeSameAs(PipelineRunModes.NotFound);
    }
}
