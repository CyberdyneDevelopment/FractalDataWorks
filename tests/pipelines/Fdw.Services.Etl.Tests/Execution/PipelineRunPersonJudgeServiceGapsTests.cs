using System;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-6528: a host that runs pipelines proves, when it initializes, that it registers every service the judge of a person's run needs, so it does not start and then
/// refuse every run for a person. A host that does not run pipelines (the scheduler, the API) is not asked.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunPersonJudgeServiceGapsTests : IDisposable
{
    private readonly string? _before = PipelineTypes.OperationalConnection;

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    private static ServiceProvider Host(params Type[] registered)
    {
        var services = new ServiceCollection();
        foreach (var service in registered)
            services.AddSingleton(service, _ => Mock.Of<object>());
        return services.BuildServiceProvider();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatRunsPipelinesAndRegistersNoJudgeServiceNamesEachOneAndDoesNotStart()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        using var host = Host();

        var gaps = PipelineRunPerson.JudgeServiceGaps(host, NullLogger.Instance).ToList();

        gaps.Select(g => g.Code).ShouldBe(["RUNPERSON-60838", "RUNPERSON-60838", "RUNPERSON-60838", "RUNPERSON-60838"]);
        gaps.Select(g => g.Message).ShouldContain(m => m.Contains(nameof(ITenantProvider), StringComparison.Ordinal));
        gaps.Select(g => g.Message).ShouldContain(m => m.Contains(nameof(IIssuanceEligibility), StringComparison.Ordinal));
        gaps.Select(g => g.Message).ShouldContain(m => m.Contains(nameof(IUserPermissions), StringComparison.Ordinal));
        gaps.Select(g => g.Message).ShouldContain(m => m.Contains(nameof(IRoleGrants), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatRunsPipelinesAndRegistersOnlyTheTenantProviderIsToldTheOtherThree()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        using var host = Host(typeof(ITenantProvider));

        var gaps = PipelineRunPerson.JudgeServiceGaps(host, NullLogger.Instance).ToList();

        gaps.Count.ShouldBe(3);
        gaps.Select(g => g.Message).ShouldNotContain(m => m.Contains(nameof(ITenantProvider), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatRunsPipelinesAndRegistersAllFourIsNotTold()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        using var host = Host(typeof(IIssuanceEligibility), typeof(ITenantProvider), typeof(IUserPermissions), typeof(IRoleGrants));

        PipelineRunPerson.JudgeServiceGaps(host, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatRunsNoPipelinesIsNotAskedForTheJudgeServices()
    {
        PipelineTypes.OperationalConnection = null;
        using var host = Host();

        PipelineRunPerson.JudgeServiceGaps(host, NullLogger.Instance).ShouldBeEmpty();
    }
}
