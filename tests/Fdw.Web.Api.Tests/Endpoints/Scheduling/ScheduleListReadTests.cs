using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Scheduling;

/// <summary>
/// The schedule list shows what the single read shows (FDW-1168): a collection below the depth a read asked for comes back empty,
/// so the list must read as deep as the members its rows carry (run parameters and run roles are child collections).
/// </summary>
public sealed class ScheduleListReadTests
{
    private sealed class ProbeList(IScheduleConfigurationProvider provider)
        : ListSchedulesEndpointBase(NullLogger<ListSchedulesEndpointBase>.Instance, provider)
    {
        public Task<IGenericResult<List<ScheduleSummaryDto>>> Load(CancellationToken cancellationToken) =>
            LoadItems(new ListSchedulesRequest(), cancellationToken);
    }

    private static ScheduleImplementationConfiguration Schedule(bool childrenRead)
    {
        var schedule = new ScheduleImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "UsgsEveryFifteenMinutes",
            PipelineId = Guid.NewGuid(),
            PipelineName = "Usgs",
            Implementation = "Interval",
            IntervalSeconds = 900
        };
        if (childrenRead)
        {
            schedule.ReplaceRunRoles(["Admin"]);
            schedule.Parameters["Region"] = "West";
        }

        return schedule;
    }

    // A provider that behaves as the real one does: the children are there only when the read asked for them.
    private static ProbeList ListOver(out Mock<IScheduleConfigurationProvider> provider)
    {
        provider = new Mock<IScheduleConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int levels, CancellationToken _) => GenericResult<IReadOnlyList<IScheduleImplementationConfiguration>>.Success(
                [Schedule(childrenRead: levels >= ConfigurationLevels.Children)]));
        return new ProbeList(provider.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AListedScheduleShowsTheRolesAndParametersTheSingleReadShows()
    {
        var list = ListOver(out _);

        var result = await list.Load(TestContext.Current.CancellationToken);

        var row = result.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        row.RunRoles.ShouldNotBeNull().ShouldBe(["Admin"]);
        row.Parameters.ShouldNotBeNull().ShouldHaveSingleItem().Name.ShouldBe("Region");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheListReadsAtTheDepthTheMapperNeeds()
    {
        var list = ListOver(out var provider);

        await list.Load(TestContext.Current.CancellationToken);

        provider.Verify(p => p.Get(ScheduleDtoMapper.ReadLevels, It.IsAny<CancellationToken>()), Times.Once);
        ScheduleDtoMapper.ReadLevels.ShouldBeGreaterThanOrEqualTo(ConfigurationLevels.Children);
    }
}
