using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using NodeListPage = Fdw.UI.Pages.EtlProjects.Pages.Orchestration.NodeListPage;
using NodeTreeEditorPage = Fdw.UI.Pages.EtlProjects.Pages.Orchestration.NodeTreeEditorPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Orchestration;

/// <summary>
/// Component tests for the orchestration pages (NodeList.razor and NodeTreeEditor.razor). Relocated
/// from reference-ui's orchestration tests; both pages are rendered directly with a SEEDED
/// <see cref="OrchestrationNodeContext"/> (the shared OrchestrationNodeProvider swapped for a stub).
/// NodeList iterates <c>ctx.FilteredRootNodes</c>; NodeTreeEditor populates its form from
/// <c>ctx.CurrentNode</c> when an Id parameter is supplied.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class OrchestrationPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static OrchestrationNodeConfiguration Node(string name, bool enabled = true,
        string? description = null, string? stagePolicy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = enabled,
            Description = description,
            StageFailurePolicy = stagePolicy,
        };

    private IRenderedComponent<NodeListPage> RenderList(OrchestrationNodeContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<OrchestrationNodeProvider, OrchestrationNodeContext>(context));
        return _ctx.Render<NodeListPage>();
    }

    private IRenderedComponent<NodeTreeEditorPage> RenderEditor(OrchestrationNodeContext context, Guid? id)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<OrchestrationNodeProvider, OrchestrationNodeContext>(context));
        return id.HasValue
            ? _ctx.Render<NodeTreeEditorPage>(p => p.Add(x => x.Id, id))
            : _ctx.Render<NodeTreeEditorPage>();
    }

    // ── NodeList ───────────────────────────────────────────────────────────────

    [Fact]
    public void ListRendersErrorBannerWhenErrorMessageSet()
    {
        // Why: current markup emits the error banner as class "card b-fail" (the old reference-ui
        // test asserted a stale "border-red-700" class the page no longer emits).
        var cut = RenderList(new OrchestrationNodeContext { LastResult = GenericResult.Failure(new GenericMessage("node load failed")) });
        cut.Markup.ShouldContain("node load failed");
        cut.FindAll("div").ShouldContain(d => d.ClassList.Contains("b-fail"));
    }

    [Fact]
    public void ListRendersEmptyRowWhenNoNodes()
    {
        var cut = RenderList(new OrchestrationNodeContext { FilteredRootNodes = [] });
        cut.Markup.ShouldContain("No orchestration nodes found.");
    }

    [Fact]
    public void ListRendersBadgesAndPolicyAndInheritFallback()
    {
        var enabled = Node("on-node", enabled: true, stagePolicy: "HaltProject");
        var disabled = Node("off-node", enabled: false, stagePolicy: null);
        var cut = RenderList(new OrchestrationNodeContext { FilteredRootNodes = [enabled, disabled] });

        cut.FindAll("span").ShouldContain(s => s.ClassList.Contains("b-ok"));
        cut.FindAll("span").ShouldContain(s => s.ClassList.Contains("b-idle"));
        cut.Markup.ShouldContain("HaltProject");
        cut.Markup.ShouldContain("inherit");
    }

    [Fact]
    public void ListSearchFiltersRows()
    {
        var keep = Node("keeper-node");
        var cut = RenderList(new OrchestrationNodeContext
        {
            RootNodes = [keep, Node("hidden-node")],
            FilteredRootNodes = [keep],
            SearchString = "keep",
        });

        cut.Markup.ShouldContain("keeper-node");
        cut.Markup.ShouldNotContain("hidden-node");
    }

    [Fact]
    public void ListDeleteButtonDeletesNode()
    {
        Guid? deleted = null;
        var node = Node("deleteme");
        var cut = RenderList(new OrchestrationNodeContext
        {
            FilteredRootNodes = [node],
            OnDeleteNode = id =>
            {
                deleted = id;
                return Task.FromResult(true);
            },
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();
        deleted.ShouldBe(node.Id);
    }

    // ── NodeTreeEditor ───────────────────────────────────────────────────────────

    [Fact]
    public void EditorNewModeRendersCreateMarkup()
    {
        var cut = RenderEditor(new OrchestrationNodeContext(), id: null);
        cut.Markup.ShouldContain("New Node");
        cut.Markup.ShouldContain("Create Node");
        cut.Markup.ShouldContain("Ordinal");
    }

    [Fact]
    public void EditorRendersThreePolicyCheckboxes()
    {
        var cut = RenderEditor(new OrchestrationNodeContext(), id: null);
        cut.Markup.ShouldContain("Require approval to run");
        cut.Markup.ShouldContain("Allow cross-tenant pipelines");
        cut.Markup.ShouldContain("Allow resume from checkpoint");
    }

    [Fact]
    public void EditorEditModeRendersSaveMarkup()
    {
        var node = Node("existing-node");
        var cut = RenderEditor(new OrchestrationNodeContext { CurrentNode = node }, id: node.Id);
        cut.Markup.ShouldContain("Edit Node");
        cut.Markup.ShouldContain("Save Changes");
    }

    [Fact]
    public void EditorBlankNameSaveDoesNotCreate()
    {
        var created = false;
        var cut = RenderEditor(new OrchestrationNodeContext
        {
            OnCreateNode = _ =>
            {
                created = true;
                return Task.FromResult<OrchestrationNodeConfiguration?>(null);
            },
        }, id: null);

        // Name input left blank — clicking Create Node must not invoke the create callback.
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Node", StringComparison.Ordinal)).Click();
        created.ShouldBeFalse();
    }

    [Fact]
    public void EditorCreateWithNameInvokesCreate()
    {
        OrchestrationNodeConfiguration? created = null;
        var cut = RenderEditor(new OrchestrationNodeContext
        {
            OnCreateNode = config =>
            {
                created = config;
                return Task.FromResult<OrchestrationNodeConfiguration?>(config);
            },
        }, id: null);

        cut.Find("input[placeholder=\"node-name\"]").Change("brand-new-node");
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Node", StringComparison.Ordinal)).Click();

        created.ShouldNotBeNull();
        created!.Name.ShouldBe("brand-new-node");
    }

    [Fact]
    public void EditorEditSaveInvokesUpdate()
    {
        var node = Node("existing-node");
        Guid? updatedId = null;
        var cut = RenderEditor(new OrchestrationNodeContext
        {
            CurrentNode = node,
            OnUpdateNode = (id, config) =>
            {
                updatedId = id;
                return Task.FromResult<OrchestrationNodeConfiguration?>(config);
            },
        }, id: node.Id);

        // The bound name field is required for the save guard to pass.
        cut.Find("input[placeholder=\"node-name\"]").Change("existing-node");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Changes", StringComparison.Ordinal)).Click();

        updatedId.ShouldBe(node.Id);
    }

    public void Dispose() => _ctx.Dispose();
}
