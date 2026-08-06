using Bunit;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Components.Schedules;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using Microsoft.Extensions.DependencyInjection;
using IndexPage = Fdw.Services.Scheduling.UI.Pages.Pages.Schedules.Index;

namespace Fdw.UI.Components.Blazor.Tests.Components.Schedules;

/// <summary>
/// Component tests for the schedules <see cref="IndexPage"/> (Index.razor). Relocated from
/// reference-ui's SchedulesPageTests; the page's <c>ScheduleProvider</c> is swapped for a stub
/// rendering a SEEDED <see cref="ScheduleContext"/>. The page ALSO injects an
/// <see cref="IPipelineClient"/> and calls <c>List()</c> unconditionally in OnInitializedAsync to
/// populate its edit-form pipeline dropdown, so a Moq <see cref="IPipelineClient"/> is registered.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class ScheduleIndexPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IPipelineClient> _pipelineClient = new();

    private static ScheduleInfoDto S(string name, string pipeline = "nightly-load", bool enabled = true,
        string? cron = "0 */5 * * *", int? intervalSeconds = null) =>
        new()
        {
            Name = name,
            PipelineName = pipeline,
            IsEnabled = enabled,
            CronExpression = cron,
            IntervalSeconds = intervalSeconds,
        };

    private IRenderedComponent<IndexPage> RenderWith(ScheduleContext context)
    {
        _ctx.RegisterPageInfrastructure();
        // Why: the page's OnInitializedAsync awaits IPipelineClient.List even though the schedules
        // come from the swapped provider; register a List stub so it does not throw.
        _pipelineClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success([]));
        _ctx.Services.AddSingleton(_pipelineClient.Object);
        _ctx.ComponentFactories.Add(new ProviderFactory<ScheduleProvider, ScheduleContext>(context));
        return _ctx.Render<IndexPage>();
    }

    [Fact]
    public void RendersEmptyStateWhenNoSchedules()
    {
        var cut = RenderWith(new ScheduleContext());
        cut.Markup.ShouldContain("No schedules configured");
    }

    [Fact]
    public void RendersActiveScheduleWithCronExpression()
    {
        var schedule = S("nightly", cron: "0 0 * * *");
        var cut = RenderWith(new ScheduleContext { Schedules = [schedule], FilteredSchedules = [schedule] });
        cut.Markup.ShouldContain("nightly");
        cut.Markup.ShouldContain("0 0 * * *");
        // Why: the old reference-ui test asserted an "Active" status text. The current page shows
        // enabled state through a toggle switch whose title is "Pause" when enabled (there is no
        // literal "Active" text), so assert that instead.
        cut.FindAll("[role=button]").ShouldContain(e => e.GetAttribute("title") == "Pause");
    }

    [Fact]
    public void PausedScheduleRendersResumeToggle()
    {
        var schedule = S("paused-one", enabled: false);
        var cut = RenderWith(new ScheduleContext { Schedules = [schedule], FilteredSchedules = [schedule] });
        // Why: a disabled schedule's toggle title is "Resume" (the old "Paused" text no longer exists).
        cut.FindAll("[role=button]").ShouldContain(e => e.GetAttribute("title") == "Resume");
    }

    [Theory]
    [InlineData(30, "Every 30s")]
    [InlineData(300, "Every 5m")]
    [InlineData(7200, "Every 2h")]
    public void RendersHumanizedIntervalWhenCronNull(int seconds, string expected)
    {
        var schedule = S("interval-one", cron: null, intervalSeconds: seconds);
        var cut = RenderWith(new ScheduleContext { Schedules = [schedule], FilteredSchedules = [schedule] });
        cut.Markup.ShouldContain(expected);
    }

    [Fact]
    public void RendersManualWhenCronAndIntervalNull()
    {
        var schedule = S("manual-one", cron: null, intervalSeconds: null);
        var cut = RenderWith(new ScheduleContext { Schedules = [schedule], FilteredSchedules = [schedule] });
        cut.Markup.ShouldContain("Manual");
    }

    [Fact]
    public void ToggleInvokesOnToggleScheduleWithDisabledFlag()
    {
        var schedule = S("nightly");
        string? capturedName = null;
        bool? capturedEnabled = null;
        var context = new ScheduleContext
        {
            Schedules = [schedule],
            FilteredSchedules = [schedule],
            OnToggleSchedule = (name, enabled) =>
            {
                capturedName = name;
                capturedEnabled = enabled;
                return Task.FromResult(true);
            },
        };
        var cut = RenderWith(context);
        // Why: an enabled schedule's toggle (title="Pause") fires OnToggleSchedule(name, !IsEnabled).
        cut.FindAll("[role=button]").First(e => e.GetAttribute("title") == "Pause").Click();
        capturedName.ShouldBe("nightly");
        capturedEnabled.ShouldBe(false);
    }

    [Fact]
    public void DeleteInvokesOnDeleteScheduleWithName()
    {
        var schedule = S("nightly");
        string? capturedName = null;
        var context = new ScheduleContext
        {
            Schedules = [schedule],
            FilteredSchedules = [schedule],
            OnDeleteSchedule = name =>
            {
                capturedName = name;
                return Task.FromResult(true);
            },
        };
        var cut = RenderWith(context);
        cut.FindAll("button").First(b => b.GetAttribute("title") == "Delete").Click();
        capturedName.ShouldBe("nightly");
    }

    [Fact]
    public void EditOpensInlineFormPrefilledWithCron()
    {
        var schedule = S("nightly", cron: "0 0 * * *");
        var cut = RenderWith(new ScheduleContext { Schedules = [schedule], FilteredSchedules = [schedule] });
        cut.FindAll("button").First(b => b.GetAttribute("title") == "Edit").Click();
        cut.Markup.ShouldContain("Edit Schedule: nightly");
        cut.FindAll("input").ShouldContain(i => i.GetAttribute("value") == "0 0 * * *");
    }

    [Fact]
    public void EditSaveInvokesOnUpdateScheduleAndClosesForm()
    {
        var schedule = S("nightly", cron: "0 0 * * *");
        string? capturedName = null;
        var context = new ScheduleContext
        {
            Schedules = [schedule],
            FilteredSchedules = [schedule],
            OnUpdateSchedule = (name, _) =>
            {
                capturedName = name;
                return Task.FromResult(true);
            },
        };
        var cut = RenderWith(context);
        cut.FindAll("button").First(b => b.GetAttribute("title") == "Edit").Click();
        cut.Markup.ShouldContain("Edit Schedule: nightly");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).Click();
        capturedName.ShouldBe("nightly");
        cut.Markup.ShouldNotContain("Edit Schedule: nightly");
    }

    public void Dispose() => _ctx.Dispose();
}
