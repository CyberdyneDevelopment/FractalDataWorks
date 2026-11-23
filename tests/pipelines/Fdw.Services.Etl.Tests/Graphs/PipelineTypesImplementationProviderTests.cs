using System;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.TestDouble;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6244: a pipe.Pipeline row names an implementation, so every option of <see cref="Fdw.Services.Pipelines.PipelineTypes"/> must have registered its
/// row provider on the pipe.Pipeline domain provider under its name by the end of Initialize. The domain checks it after every option has initialized;
/// a pipeline type that sets no phase is refused, naming it, and a graph pipeline type, which registers its provider in its own Initialization, is not.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineTypesImplementationProviderTests : IClassFixture<StartedPipelineDomain>
{
    private const string ImplementationProviderNotRegistered = "PIPELINES-60837";

    private readonly StartedPipelineDomain _domain;

    public PipelineTypesImplementationProviderTests(StartedPipelineDomain domain) => _domain = domain;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void APipelineTypeThatSetsNoPhaseStopsTheHostAtInitializeNamingIt()
    {
        _domain.Configured.IsSuccess.ShouldBeTrue(_domain.Configured.CurrentMessage);
        _domain.Registered.IsSuccess.ShouldBeTrue(_domain.Registered.CurrentMessage);

        _domain.Initialized.IsFailure.ShouldBeTrue();
        // Why the type is named: the test double project holds a second type that sets no phase on purpose (PersonOnly, FDW-6137), so the host is refused for each of them and this test asks about the silent one.
        _domain.Initialized.Messages
            .Where(m => string.Equals(m.Code, ImplementationProviderNotRegistered, StringComparison.Ordinal)
                && m.Message.Contains(SilentPipelineType.OptionName, StringComparison.Ordinal))
            .ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AGraphPipelineTypeThatRegistersItsProviderInItsOwnInitializationIsNotRefused()
    {
        // Why this case: the check depends on order. Run before the options' Initialization bodies, it would report every option; only an option that
        // registers in its own Initialization shows it runs after them.
        _domain.Initialized.Messages.ShouldNotContain(m =>
            string.Equals(m.Code, ImplementationProviderNotRegistered, StringComparison.Ordinal)
            && m.Message.Contains(GraphPipelineType.OptionName, StringComparison.Ordinal));
        _domain.BuiltHost.Services.GetRequiredService<IPipelineConfigurationProvider>()
            .Implementation(GraphPipelineType.OptionName).IsSuccess.ShouldBeTrue();
    }
}
