using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// FDW-3574: <see cref="SchedulerServiceProvider"/>'s get-all answers every member its domain configuration provider
/// holds, each built through the implementation the member names. It answered an empty list before.
/// </summary>
public sealed class SchedulerServiceProviderGetAllTests
{
    private const string Implementation = "GetAllTestImplementation";

    [Fact]
    public async Task GetAllAnswersEveryStoredMember()
    {
        var first = new Mock<IFrameworkSchedulingService>().Object;
        var second = new Mock<IFrameworkSchedulingService>().Object;
        var provider = ProviderOver(("first", first), ("second", second));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([first, second]);
    }

    [Fact]
    public async Task GetAllFailsWhenAMemberFailsToBuild()
    {
        var provider = ProviderOver(("first", new Mock<IFrameworkSchedulingService>().Object), ("unbuildable", null));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // Each member is a configuration naming one implementation; a null service makes that member's
    // build fail, as an implementation provider that cannot build answers.
    private static SchedulerServiceProvider ProviderOver(params (string Name, IFrameworkSchedulingService? Service)[] members)
    {
        var configurations = members.Select(m =>
        {
            var configuration = new Mock<ISchedulerImplementationConfiguration>();
            configuration.SetupProperty(c => c.Name, m.Name);
            configuration.SetupProperty(c => c.Implementation, Implementation);
            return configuration.Object;
        }).ToList();

        var store = new Mock<IDomainConfigurationProvider<ISchedulerImplementationConfiguration>>();
        store.Setup(s => s.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ISchedulerImplementationConfiguration>>.Success(configurations));

        var implementation = new Mock<IImplementationServiceProvider<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>>();
        implementation.Setup(i => i.Create(It.IsAny<ISchedulerImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ISchedulerImplementationConfiguration configuration, CancellationToken _) =>
                members.Single(m => string.Equals(m.Name, configuration.Name, StringComparison.Ordinal)).Service is { } service
                    ? GenericResult<IFrameworkSchedulingService>.Success(service)
                    : GenericResult<IFrameworkSchedulingService>.Failure(new Fdw.Messages.GenericMessage("cannot build")));

        var provider = new SchedulerServiceProvider(NullLogger<SchedulerServiceProvider>.Instance);
        provider.Register(store.Object).IsSuccess.ShouldBeTrue();
        provider.Register(Implementation, () => implementation.Object).IsSuccess.ShouldBeTrue();
        return provider;
    }
}
