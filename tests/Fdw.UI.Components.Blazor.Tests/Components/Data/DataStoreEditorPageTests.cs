using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Data.Components.DataStores;
using Fdw.UI.Pages.Data.Pages;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Components.ConnectionWizard;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.ConnInfra;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Tests for the heaviest FDW page, <see cref="DataStoreEditor"/> — a 3-step wizard (Basic Info /
/// Store Type / Containers) with an inline "New Connection" modal hosting a nested ConnectionWizard.
/// Relocated from reference-ui's DataStoreEditorPageTests: the deep step/field/validation/container/
/// modal assertions were reframed in the app to a host smoke, and the equivalent (or stronger)
/// coverage now runs here against the FDW page rendered through stubbed DataStoreEditorProvider /
/// ConnectionWizardProvider components.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataStoreEditorPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(DataStoreEditorContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<DataStoreEditorProvider, DataStoreEditorContext>(seed));

    private void SwapInnerWizard(ConnectionWizardContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<ConnectionWizardProvider, ConnectionWizardContext>(seed));

    private static ConnectionPayload Conn(string name, string type = "MsSql") =>
        new() { Id = Guid.NewGuid(), Name = name, ConnectionType = type, LastTestSuccess = true };

    private static ConfigurationTypeSummary StoreType(string typeName, string display) =>
        new() { TypeName = typeName, DisplayName = display };

    private IRenderedComponent<DataStoreEditorPage> RenderEditor(string? name = null) =>
        _ctx.Render<DataStoreEditorPage>(p => { if (name is not null) { p.Add(x => x.Name, name); } });

    // Header / step indicator ───────────────────────────────────────────────────

    [Fact]
    public void NewRendersHeaderAndStepIndicator()
    {
        Swap(new DataStoreEditorContext { Step = 0, IsFirstStep = true, Connections = [Conn("A")] });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("New DataStore");
        cut.FindAll(".stepper").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Basic Info");
    }

    [Fact]
    public void LoadingStep0NoConnectionsShowsSpinner()
    {
        Swap(new DataStoreEditorContext { Step = 0, IsLoading = true, Connections = [] });
        var cut = RenderEditor();
        cut.FindAll(".spin").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ErrorMessageRendersBanner()
    {
        Swap(new DataStoreEditorContext { Step = 0, Connections = [Conn("A")], LastResult = GenericResult.Failure(new GenericMessage("save failed")) });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("save failed");
    }

    // Step 0: Basic Info ──────────────────────────────────────────────────────────

    [Fact]
    public void Step0RendersNameDisplayConnectionFields()
    {
        Swap(new DataStoreEditorContext { Step = 0, Connections = [Conn("PROD_SQL", "MsSql")] });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("DataStore name");
        cut.Markup.ShouldContain("Display name");
        cut.Markup.ShouldContain("Connection");
        cut.Markup.ShouldContain("PROD_SQL");
        cut.Markup.ShouldContain("+ New Connection");
    }

    [Fact]
    public void Step0EditNameDisabled()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 0,
            Connections = [Conn("A")],
            Form = new DataStoreEditorModel { Name = "Sales" }
        });
        var cut = RenderEditor("Sales");
        cut.Find("input[placeholder='e.g. PROD_SALES']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Step0NoConnectionsShowsLoadingHint()
    {
        Swap(new DataStoreEditorContext { Step = 0, Connections = [] });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("Loading connections...");
    }

    [Fact]
    public void Step0LoadingCapabilitiesShowsHint()
    {
        Swap(new DataStoreEditorContext { Step = 0, Connections = [Conn("A")], IsLoadingCapabilities = true });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("Loading capabilities...");
    }

    [Fact]
    public async Task Step0NameInputBindsToForm()
    {
        var ctx = new DataStoreEditorContext { Step = 0, Connections = [Conn("A")] };
        Swap(ctx);
        var cut = RenderEditor();
        cut.Find("input[placeholder='e.g. PROD_SALES']").Change("MYSTORE");
        await Task.Yield();
        ctx.Form.Name.ShouldBe("MYSTORE");
    }

    [Fact]
    public async Task Step0ConnectionSelectInvokesOnConnectionChanged()
    {
        string? changed = null;
        Swap(new DataStoreEditorContext
        {
            Step = 0,
            Connections = [Conn("A"), Conn("B")],
            OnConnectionChanged = c => { changed = c; return Task.CompletedTask; }
        });
        var cut = RenderEditor();
        cut.FindAll("select").First(s => s.InnerHtml.Contains("Select Connection", StringComparison.Ordinal)).Change("B");
        await Task.Yield();
        changed.ShouldBe("B");
    }

    [Fact]
    public void Step0NextDisabledWhenNameOrConnectionMissing()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 0,
            IsFirstStep = true,
            Connections = [Conn("A")],
            Form = new DataStoreEditorModel { Name = "", ConnectionName = "" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Step0NextEnabledWhenNameAndConnectionSet()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 0,
            IsFirstStep = true,
            Connections = [Conn("A")],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Step0NoPreviousButtonOnFirstStep()
    {
        Swap(new DataStoreEditorContext { Step = 0, IsFirstStep = true, Connections = [Conn("A")] });
        var cut = RenderEditor();
        cut.FindAll("button").Any(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).ShouldBeFalse();
    }

    // Step 1: Store Type & Write Config ───────────────────────────────────────────

    [Fact]
    public void Step1RendersStoreTypeDropdownWhenTypesPresent()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [StoreType("SqlServer", "SQL Server")],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", ConnectionTypeName = "MsSql" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("Store type");
        cut.Markup.ShouldContain("SQL Server");
    }

    [Fact]
    public void Step1RendersManualStoreTypeInputWhenNoTypes()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("types could not be loaded");
    }

    [Fact]
    public void Step1RendersWriteModeDropdownWhenCapabilitiesHaveWriteModes()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [StoreType("SqlServer", "SQL Server")],
            Capabilities = new ConnectionTypeCapabilitiesPayload { WriteModes = ["Append", "Overwrite"] },
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("Write mode");
        cut.Markup.ShouldContain("Append");
        cut.Markup.ShouldContain("Overwrite");
    }

    [Fact]
    public void Step1RendersStep0SelectionSummary()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [StoreType("SqlServer", "SQL Server")],
            Form = new DataStoreEditorModel { Name = "SalesStore", ConnectionName = "PROD_SQL", ConnectionTypeName = "MsSql" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("SalesStore");
        cut.Markup.ShouldContain("PROD_SQL");
        cut.Markup.ShouldContain("MsSql");
    }

    [Fact]
    public void Step1RendersManualStoreTypeInputForUnknownConnectionType()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", ConnectionTypeName = "HttpRest" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("Store type");
        cut.Markup.ShouldContain("types could not be loaded");
    }

    [Fact]
    public void Step1NextDisabledWhenStoreTypeMissing()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [StoreType("SqlServer", "SQL Server")],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task Step1PreviousButtonInvokesOnPreviousStep()
    {
        var back = false;
        Swap(new DataStoreEditorContext
        {
            Step = 1,
            DataStoreTypes = [StoreType("SqlServer", "SQL Server")],
            OnPreviousStep = () => back = true,
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).Click();
        await Task.Yield();
        back.ShouldBeTrue();
    }

    // Step 2: Container Management ─────────────────────────────────────────────────

    [Fact]
    public void Step2EmptyContainersShowsHint()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("No containers defined yet");
        cut.Markup.ShouldContain("Containers (0)");
    }

    [Fact]
    public void Step2RendersExistingPaths()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [new DataPathRequest { Name = "Primary", PhysicalPath = "dbo.Orders", Description = "main" }],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("Primary");
        cut.Markup.ShouldContain("dbo.Orders");
        cut.Markup.ShouldContain("main");
        cut.Markup.ShouldContain("Containers (1)");
    }

    [Fact]
    public void Step2AddContainerButtonDisabledWhenFieldsEmpty()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("+ Add Container", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task Step2AddContainerInvokesOnAddPath()
    {
        DataPathRequest? added = null;
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [],
            OnAddPath = p => added = p,
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.Find("input[placeholder='e.g. Primary']").Change("C1");
        cut.Find("input[placeholder='e.g. dbo.TableName']").Change("dbo.X");
        cut.FindAll("button").First(b => b.TextContent.Contains("+ Add Container", StringComparison.Ordinal)).Click();
        await Task.Yield();
        added.ShouldNotBeNull();
        added!.Name.ShouldBe("C1");
        added.PhysicalPath.ShouldBe("dbo.X");
    }

    [Fact]
    public void Step2PathFormatPlaceholderUsesCapabilities()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [],
            Capabilities = new ConnectionTypeCapabilitiesPayload { PathFormats = ["{schema}.{table}"] },
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.Markup.ShouldContain("{schema}.{table}");
    }

    [Fact]
    public async Task Step2RemovePathInvokesOnRemovePath()
    {
        int? removed = null;
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [new DataPathRequest { Name = "P1", PhysicalPath = "dbo.X" }],
            OnRemovePath = i => removed = i,
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.InnerHtml.Contains("M6 18L18 6", StringComparison.Ordinal)).Click();
        await Task.Yield();
        removed.ShouldBe(0);
    }

    [Fact]
    public async Task Step2SaveButtonInvokesOnSave()
    {
        var saved = false;
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [],
            OnSave = () => { saved = true; return Task.CompletedTask; },
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("Create DataStore", StringComparison.Ordinal)).Click();
        await Task.Yield();
        saved.ShouldBeTrue();
    }

    [Fact]
    public void Step2SavingDisablesSaveButton()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            IsSaving = true,
            Paths = [],
            Form = new DataStoreEditorModel { Name = "S", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("Saving...", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Step2EditShowsSaveChangesLabel()
    {
        Swap(new DataStoreEditorContext
        {
            Step = 2,
            IsLastStep = true,
            Paths = [],
            Form = new DataStoreEditorModel { Name = "Sales", ConnectionName = "A", StoreType = "SqlServer" }
        });
        var cut = RenderEditor("Sales");
        cut.Markup.ShouldContain("Save Changes");
    }

    // Inline New-Connection modal ──────────────────────────────────────────────────

    [Fact]
    public void NewConnectionButtonOpensModalWithNestedWizard()
    {
        SwapInnerWizard(new ConnectionWizardContext { Step = 0, IsFirstStep = true, ConnectionTypes = [] });
        Swap(new DataStoreEditorContext { Step = 0, Connections = [Conn("A")] });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("+ New Connection", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("New Connection");
        cut.Markup.ShouldContain("Configure");
    }

    [Fact]
    public void NewConnectionModalCloseHidesModal()
    {
        SwapInnerWizard(new ConnectionWizardContext { Step = 0, IsFirstStep = true, ConnectionTypes = [] });
        Swap(new DataStoreEditorContext { Step = 0, Connections = [Conn("A")] });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("+ New Connection", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Configure"); // modal open
        cut.FindAll("button").First(b => b.InnerHtml.Contains("M6 18L18 6", StringComparison.Ordinal)).Click();
        cut.FindAll("h3").Any(h => h.TextContent.Contains("Configure", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public void NewConnectionModalNestedWizardCompleteShowsSuccessPanel()
    {
        SwapInnerWizard(new ConnectionWizardContext { IsComplete = true });
        Swap(new DataStoreEditorContext { Step = 0, Connections = [Conn("A")] });
        var cut = RenderEditor();
        cut.FindAll("button").First(b => b.TextContent.Contains("+ New Connection", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Connection created successfully");
    }

    public void Dispose() => _ctx.Dispose();
}
