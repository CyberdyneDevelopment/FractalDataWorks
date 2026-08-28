using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Clients;
using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using ProjectListPage = Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectListPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Projects;

/// <summary>
/// Component tests for the ProjectList page (ProjectList.razor), the tabular project list view.
/// Relocated from reference-ui's ProjectList tests; the page is rendered directly with a SEEDED
/// <see cref="ProjectContext"/> (its provider swapped for a stub). The table iterates
/// <c>ctx.FilteredProjects</c>, so seeding that list controls which rows render (mirroring search).
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class ProjectListPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static ProjectConfiguration Project(string name, bool enabled = true,
        string? description = null, string? stagePolicy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = enabled,
            Description = description,
            StageFailurePolicy = stagePolicy,
        };

    private IRenderedComponent<ProjectListPage> RenderWith(ProjectContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<ProjectProvider, ProjectContext>(context));
        return _ctx.Render<ProjectListPage>();
    }

    [Fact]
    public void RendersErrorBannerWhenErrorMessageSet()
    {
        var cut = RenderWith(new ProjectContext { LastResult = GenericResult.Failure(new GenericMessage("list failed")) });
        cut.Markup.ShouldContain("list failed");
        cut.FindAll("div").ShouldContain(d => d.ClassList.Contains("b-fail"));
    }

    [Fact]
    public void RendersEnabledBadgePolicyAndDescription()
    {
        var project = Project("enabled-proj", enabled: true, description: "the desc", stagePolicy: "HaltProject");
        var cut = RenderWith(new ProjectContext { FilteredProjects = [project] });

        cut.FindAll("span").ShouldContain(s => s.ClassList.Contains("b-ok"));
        cut.Markup.ShouldContain("Enabled");
        cut.Markup.ShouldContain("the desc");
        cut.Markup.ShouldContain("HaltProject");
    }

    [Fact]
    public void RendersDisabledBadgeWithInheritAndDashFallbacks()
    {
        // Null StageFailurePolicy renders "inherit"; null Description renders "-".
        var project = Project("disabled-proj", enabled: false, description: null, stagePolicy: null);
        var cut = RenderWith(new ProjectContext { FilteredProjects = [project] });

        cut.FindAll("span").ShouldContain(s => s.ClassList.Contains("b-idle"));
        cut.Markup.ShouldContain("Disabled");
        cut.Markup.ShouldContain("inherit");
        // Null Description renders "-" in the description cell (class "mut").
        cut.FindAll("td.mut").ShouldContain(td => string.Equals(td.TextContent.Trim(), "-", StringComparison.Ordinal));
    }

    [Fact]
    public void SearchFiltersRowsByName()
    {
        // The page renders ctx.FilteredProjects; seeding the filtered subset asserts which rows show.
        var keep = Project("keeper");
        var cut = RenderWith(new ProjectContext
        {
            Projects = [keep, Project("hidden")],
            FilteredProjects = [keep],
            SearchString = "keep",
        });

        cut.Markup.ShouldContain("keeper");
        cut.Markup.ShouldNotContain("hidden");
    }

    [Fact]
    public void RendersEmptyFilterRowWhenNoMatches()
    {
        var cut = RenderWith(new ProjectContext { FilteredProjects = [] });
        cut.Markup.ShouldContain("No projects found.");
    }

    [Fact]
    public void RunButtonTriggersProject()
    {
        var triggered = string.Empty;
        var project = Project("runme");
        var cut = RenderWith(new ProjectContext
        {
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

    [Fact]
    public void DeleteButtonDeletesProject()
    {
        Guid? deleted = null;
        var project = Project("deleteme");
        var cut = RenderWith(new ProjectContext
        {
            FilteredProjects = [project],
            OnDeleteProject = id =>
            {
                deleted = id;
                return Task.FromResult(true);
            },
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();
        deleted.ShouldBe(project.Id);
    }

    public void Dispose() => _ctx.Dispose();
}
