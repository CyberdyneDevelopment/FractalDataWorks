using Bunit;
using Fdw.Operations.Clients.Models;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Components.Schedules;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using Microsoft.Extensions.DependencyInjection;
using NewPage = Fdw.UI.Pages.Scheduling.Pages.Schedules.NewSchedulePage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Schedules;

/// <summary>
/// Component tests for the schedules <see cref="NewPage"/> (New.razor). Relocated from reference-ui's
/// NewSchedulePageTests; the page's <c>ScheduleProvider</c> is swapped for a stub rendering a SEEDED
/// <see cref="ScheduleContext"/>. The page ALSO injects <see cref="IPipelineClient"/> (its pipeline
/// dropdown is sourced from <c>IPipelineClient.List</c>, NOT the context) and a NavigationManager
/// (bUnit supplies a fake), so a Moq <see cref="IPipelineClient"/> is registered.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class ScheduleNewPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IPipelineClient> _pipelineClient = new();

    private static PipelineSummaryResponse P(string name, string type = "BatchCopy") =>
        new() { Id = Guid.NewGuid(), Name = name, PipelineType = type };

    private void SeedPipelines(params PipelineSummaryResponse[] pipelines) =>
        _pipelineClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success(pipelines));

    private IRenderedComponent<NewPage> RenderWith(ScheduleContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.Services.AddSingleton(_pipelineClient.Object);
        _ctx.ComponentFactories.Add(new ProviderFactory<ScheduleProvider, ScheduleContext>(context));
        return _ctx.Render<NewPage>();
    }

    [Fact]
    public void RendersCronFieldsByDefault()
    {
        SeedPipelines();
        var cut = RenderWith(new ScheduleContext());
        cut.Markup.ShouldContain("Cron Expression");
        cut.FindAll("input").ShouldContain(i => i.GetAttribute("placeholder") == "0 */5 * * *");
    }

    [Fact]
    public void StaticDropdownOffersAllSchedulerTypes()
    {
        SeedPipelines();
        var cut = RenderWith(new ScheduleContext());
        var options = cut.FindAll("option").Select(o => o.GetAttribute("value")).ToList();
        options.ShouldContain("Cron");
        options.ShouldContain("Interval");
        options.ShouldContain("OneTime");
        options.ShouldContain("Event");
    }

    [Fact]
    public void LoadedScheduleTypesReplaceStaticDropdown()
    {
        SeedPipelines();
        var context = new ScheduleContext
        {
            ScheduleTypes =
            [
                new ConfigurationTypeSummary { TypeName = "Cron", DisplayName = "Cron Trigger", Category = "Schedule" },
            ],
        };
        var cut = RenderWith(context);
        cut.Markup.ShouldContain("Cron Trigger");
    }

    [Fact]
    public void PipelineDropdownListsPipelinesFromClient()
    {
        SeedPipelines(P("nightly-load"), P("stream-sync"));
        var cut = RenderWith(new ScheduleContext());
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("nightly-load");
            cut.Markup.ShouldContain("stream-sync");
        });
    }

    [Fact]
    public void SelectingIntervalShowsIntervalFieldsAndHidesCron()
    {
        SeedPipelines();
        var cut = RenderWith(new ScheduleContext());
        cut.Find("select").Change("Cron"); // ensure type select is the schedule-type select
        var typeSelect = cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "Interval"));
        typeSelect.Change("Interval");
        cut.Markup.ShouldContain("Interval");
        cut.Markup.ShouldNotContain("Cron Expression");
    }

    [Fact]
    public void SelectingOneTimeShowsDateAndTimeFields()
    {
        SeedPipelines();
        var cut = RenderWith(new ScheduleContext());
        var typeSelect = cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "OneTime"));
        typeSelect.Change("OneTime");
        cut.Markup.ShouldContain("Execution Date");
        cut.Markup.ShouldContain("Execution Time");
    }

    [Fact]
    public void SelectingEventShowsEventNameField()
    {
        SeedPipelines();
        var cut = RenderWith(new ScheduleContext());
        var typeSelect = cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "Event"));
        typeSelect.Change("Event");
        cut.Markup.ShouldContain("Event Name");
    }

    [Fact]
    public void CreateWithoutNameOrPipelineShowsValidationAndDoesNotFireCallback()
    {
        SeedPipelines(P("nightly-load"));
        var fired = false;
        var context = new ScheduleContext
        {
            OnCreateSchedule = _ =>
            {
                fired = true;
                return Task.FromResult(true);
            },
        };
        var cut = RenderWith(context);
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Schedule", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Name and Pipeline are required.");
        fired.ShouldBeFalse();
    }

    [Fact]
    public void CreateSuccessFiresOnCreateScheduleWithCronType()
    {
        SeedPipelines(P("nightly-load"));
        CreateScheduleClientRequest? captured = null;
        var context = new ScheduleContext
        {
            OnCreateSchedule = req =>
            {
                captured = req;
                return Task.FromResult(true);
            },
        };
        var cut = RenderWith(context);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("nightly-load"));

        cut.Find("input[placeholder='my-schedule']").Change("daily-job");
        cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "nightly-load"))
            .Change("nightly-load");
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Schedule", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            captured.ShouldNotBeNull();
            captured!.Name.ShouldBe("daily-job");
            captured.PipelineName.ShouldBe("nightly-load");
            captured.SchedulerType.ShouldBe("Cron");
        });
    }

    [Fact]
    public void CreateFailureShowsErrorMessage()
    {
        SeedPipelines(P("nightly-load"));
        var context = new ScheduleContext
        {
            OnCreateSchedule = _ => Task.FromResult(false),
        };
        var cut = RenderWith(context);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("nightly-load"));

        cut.Find("input[placeholder='my-schedule']").Change("daily-job");
        cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "nightly-load"))
            .Change("nightly-load");
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Schedule", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to create schedule."));
    }

    [Fact]
    public void IntervalCreateComputesSecondsFromMinutes()
    {
        SeedPipelines(P("nightly-load"));
        CreateScheduleClientRequest? captured = null;
        var context = new ScheduleContext
        {
            OnCreateSchedule = req =>
            {
                captured = req;
                return Task.FromResult(true);
            },
        };
        var cut = RenderWith(context);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("nightly-load"));

        cut.Find("input[placeholder='my-schedule']").Change("daily-job");
        cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "nightly-load"))
            .Change("nightly-load");
        // Why: default interval value is 5 with unit "Minutes" -> 300 seconds.
        cut.FindAll("select")
            .First(s => s.QuerySelectorAll("option").Any(o => o.GetAttribute("value") == "Interval"))
            .Change("Interval");
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Schedule", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            captured.ShouldNotBeNull();
            captured!.SchedulerType.ShouldBe("Interval");
            captured.IntervalSeconds.ShouldBe(300);
        });
    }

    public void Dispose() => _ctx.Dispose();
}
