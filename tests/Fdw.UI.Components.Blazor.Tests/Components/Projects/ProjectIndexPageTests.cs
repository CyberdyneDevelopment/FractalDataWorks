using Bunit;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Clients;
using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using ProjectIndexPage = Fdw.Services.Etl.Projects.UI.Pages.Pages.Projects.ProjectIndex;

namespace Fdw.UI.Components.Blazor.Tests.Components.Projects;

/// <summary>
/// Component tests for the ProjectIndex page (ProjectIndex.razor), the project tree view. Relocated
/// from reference-ui's ProjectIndex tests, which drove the hosted page via HTTP; here the page is
/// rendered directly with a SEEDED <see cref="ProjectContext"/> (its provider swapped for a stub),
/// asserting the same rendered markup at the FDW layer. The page iterates <c>ctx.Projects</c>, so
/// the seeded list goes there (FilteredProjects is seeded identically for safety).
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class ProjectIndexPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static ProjectConfiguration Project(string name, bool enabled = true,
        IList<StageConfiguration>? stages = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = enabled,
            Stages = stages ?? new List<StageConfiguration>(),
        };

    private static StageConfiguration Stage(string name, int ordinal, IList<StepConfiguration>? steps = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Ordinal = ordinal,
            Steps = steps ?? new List<StepConfiguration>(),
        };

    private static StepConfiguration Step(string name, int ordinal, int pipelineCount) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Ordinal = ordinal,
            Pipelines = BuildPipelines(pipelineCount),
        };

    private static List<StepPipelineMembershipConfiguration> BuildPipelines(int count)
    {
        var list = new List<StepPipelineMembershipConfiguration>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new StepPipelineMembershipConfiguration { Id = Guid.NewGuid(), Name = $"p{i}" });
        }

        return list;
    }

    private IRenderedComponent<ProjectIndexPage> RenderWith(ProjectContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<ProjectProvider, ProjectContext>(context));
        return _ctx.Render<ProjectIndexPage>();
    }

    [Fact]
    public void RendersErrorBannerWhenErrorMessageSet()
    {
        // Why: current markup emits the error banner as class "card b-fail" (the old reference-ui
        // test asserted a stale "border-red-700" class the page no longer emits).
        var cut = RenderWith(new ProjectContext { ErrorMessage = "load failed" });
        cut.Markup.ShouldContain("load failed");
        cut.FindAll("div").ShouldContain(d => d.ClassList.Contains("b-fail"));
    }

    [Fact]
    public void RendersEmptyStateWhenNoProjects()
    {
        var cut = RenderWith(new ProjectContext { Projects = [], FilteredProjects = [] });
        cut.Markup.ShouldContain("No projects defined yet.");
    }

    [Fact]
    public void RendersProjectRowNames()
    {
        var projects = new[] { Project("alpha-project"), Project("beta-project") };
        var cut = RenderWith(new ProjectContext { Projects = projects, FilteredProjects = projects });
        cut.Markup.ShouldContain("alpha-project");
        cut.Markup.ShouldContain("beta-project");
    }

    [Fact]
    public void RendersDisabledBadgeForDisabledProject()
    {
        var projects = new[] { Project("off-project", enabled: false) };
        var cut = RenderWith(new ProjectContext { Projects = projects, FilteredProjects = projects });
        cut.FindAll("span").ShouldContain(s => s.ClassList.Contains("b-idle"));
        cut.Markup.ShouldContain("disabled");
    }

    [Fact]
    public void ExpandProjectRevealsStages()
    {
        var stage = Stage("ingest", 0);
        var project = Project("p", stages: [stage]);
        var cut = RenderWith(new ProjectContext { Projects = [project], FilteredProjects = [project] });

        cut.Markup.ShouldNotContain("ingest");
        cut.FindAll("button").First(b =>
            b.GetAttribute("aria-label") is "Expand p").Click();
        cut.Markup.ShouldContain("ingest");
    }

    [Fact]
    public void ExpandStageRevealsStepsWithPipelinePluralization()
    {
        var step1 = Step("solo-step", 0, pipelineCount: 1);
        var step2 = Step("multi-step", 1, pipelineCount: 3);
        var stage = Stage("transform", 0, steps: [step1, step2]);
        var project = Project("p", stages: [stage]);
        var cut = RenderWith(new ProjectContext { Projects = [project], FilteredProjects = [project] });

        // Expand the project to reveal the stage.
        cut.FindAll("button").First(b => b.GetAttribute("aria-label") is "Expand p").Click();
        cut.Markup.ShouldContain("transform");
        cut.Markup.ShouldNotContain("solo-step");

        // Expand the stage (its toggle is the first ghost button with the chevron-right path inside
        // the stage row group) to reveal steps. The stage toggle has no aria-label, so find by the
        // stage-row chevron: it is the button whose parent contains the stage name.
        var stageToggle = cut.FindAll("button")
            .First(b => b.ParentElement is not null
                && b.ParentElement.TextContent.Contains("transform", StringComparison.Ordinal));
        stageToggle.Click();

        cut.Markup.ShouldContain("solo-step");
        cut.Markup.ShouldContain("multi-step");
        cut.Markup.ShouldContain("1 pipeline");
        cut.Markup.ShouldContain("3 pipelines");
    }

    [Fact]
    public void RunButtonTriggersProject()
    {
        var triggered = string.Empty;
        var project = Project("runme");
        var cut = RenderWith(new ProjectContext
        {
            Projects = [project],
            FilteredProjects = [project],
            OnTriggerProject = name =>
            {
                triggered = name;
                return Task.FromResult<TriggerResponse?>(new TriggerResponse { Status = "Triggered" });
            },
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("Run", StringComparison.Ordinal)).Click();
        triggered.ShouldBe("runme");
    }

    public void Dispose() => _ctx.Dispose();
}
