using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fdw.Services.Etl.Tests;

/// <summary>
/// FDW-3574: <see cref="EtlPipelineProvider"/>'s get-all answers every member its domain configuration provider
/// holds, each built through the implementation the member names. It answered an empty list before.
/// </summary>
public sealed class EtlPipelineProviderGetAllTests
{
    private const string Implementation = "GetAllTestImplementation";

    [Fact]
    public async Task GetAllAnswersEveryStoredMember()
    {
        var first = new Mock<IEtlPipeline>().Object;
        var second = new Mock<IEtlPipeline>().Object;
        var provider = ProviderOver(("first", first), ("second", second));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([first, second]);
    }

    [Fact]
    public async Task GetAllFailsWhenAMemberFailsToBuild()
    {
        var provider = ProviderOver(("first", new Mock<IEtlPipeline>().Object), ("unbuildable", null));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // Each member is a configuration naming one implementation; a null service makes that member's
    // build fail, as an implementation provider that cannot build answers.
    private static EtlPipelineProvider ProviderOver(params (string Name, IEtlPipeline? Service)[] members)
    {
        var configurations = members.Select(m =>
        {
            var configuration = new Mock<IPipelineImplementationConfiguration>();
            configuration.SetupProperty(c => c.Name, m.Name);
            configuration.SetupProperty(c => c.Implementation, Implementation);
            return configuration.Object;
        }).ToList();

        var store = new Mock<IDomainConfigurationProvider<IPipelineImplementationConfiguration>>();
        store.Setup(s => s.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(configurations));

        var implementation = new Mock<IImplementationServiceProvider<IEtlPipeline, IPipelineImplementationConfiguration>>();
        implementation.Setup(i => i.Create(It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPipelineImplementationConfiguration configuration, CancellationToken _) =>
                members.Single(m => string.Equals(m.Name, configuration.Name, StringComparison.Ordinal)).Service is { } service
                    ? GenericResult<IEtlPipeline>.Success(service)
                    : GenericResult<IEtlPipeline>.Failure(new Fdw.Messages.GenericMessage("cannot build")));

        var provider = new EtlPipelineProvider(NullLogger<EtlPipelineProvider>.Instance);
        provider.Register(store.Object).IsSuccess.ShouldBeTrue();
        provider.Register(Implementation, () => implementation.Object).IsSuccess.ShouldBeTrue();
        return provider;
    }
}
