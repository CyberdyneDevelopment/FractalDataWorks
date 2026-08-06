using Bunit;
using Fdw.Data.Components.DataSets;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using DataSetWizardPage = Fdw.Data.UI.Pages.Pages.DataSetWizard;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Branch-complete bUnit tests for the FDW <c>DataSetWizard</c> page
/// (<c>Fdw.Data.UI.Pages.Pages.DataSetWizard</c>). Relocated from
/// reference-ui's <c>DataSetWizardPageTests</c>.
///
/// The wizard nests two providers (<see cref="DataSetWizardProvider"/> +
/// <see cref="DataSetProvider"/>); both are swapped for concrete-subclass stubs via
/// <see cref="InheritingProviderFactory{TProvider, TStub}"/> so the page's
/// <c>@ref</c> casts succeed. Each of the six wizard steps is a render branch driven
/// by the internal <c>_activeStep</c> field, advanced via the Continue button.
/// Conditional sub-branches (import panel, capability-driven field-type /
/// connection-type lists, join linkage) are each exercised, plus the three submit
/// branches (success nav, null-result error, exception message) and step navigation.
///
/// Markup note: the live razor uses plain human-readable labels ("New DataSet",
/// "Add Field", "Continue", "Create DataSet", the <c>GetStepTitle</c> stepper, etc.),
/// not the styled all-caps tokens the original reference-ui test asserted. Selectors
/// below are aligned to the CURRENT markup while preserving each test's meaning.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataSetWizardPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private DataSetWizardContext _wizSeed = new();
    private DataSetContext _dsSeed = new();

    private void Swap(DataSetWizardContext? wizSeed = null, DataSetContext? dsSeed = null)
    {
        // Why: the wizard captures DataSetWizardProvider via @ref="_wizardProvider".
        // Use concrete subclass stubs (IS-A the real providers) so the @ref cast succeeds;
        // both nested providers are seeded via cascading values.
        _wizSeed = wizSeed ?? new DataSetWizardContext();
        _dsSeed = dsSeed ?? new DataSetContext();
        _ctx.RegisterProviderInfrastructure();
        _ctx.ComponentFactories.Add(new InheritingProviderFactory<DataSetWizardProvider, StubDataSetWizardProvider>());
        _ctx.ComponentFactories.Add(new InheritingProviderFactory<DataSetProvider, StubDataSetProvider>());
    }

    private IRenderedComponent<DataSetWizardPage> RenderWizard(Action<ComponentParameterCollectionBuilder<DataSetWizardPage>>? extra = null) =>
        _ctx.Render<DataSetWizardPage>(p =>
        {
            p.AddCascadingValue(new DataSetWizardContextSeed { Value = _wizSeed });
            p.AddCascadingValue(new DataSetContextSeed { Value = _dsSeed });
            extra?.Invoke(p);
        });

    private static ConfigurationTypeSummary Type(string typeName, string? display = null) => new()
    {
        TypeName = typeName,
        DisplayName = display ?? string.Empty
    };

    private static void Advance(IRenderedComponent<DataSetWizardPage> cut, int times)
    {
        for (var i = 0; i < times; i++)
        {
            // Why: the "Continue" next button text contains a trailing arrow entity; match on the word only.
            cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).Click();
        }
    }

    // ── Header / step indicator ─────────────────────────────────────────────

    [Fact]
    public void NewModeRendersNewDataSetHeaderAndGeneralStep()
    {
        Swap();
        var cut = RenderWizard();
        cut.Markup.ShouldContain("New DataSet");
        // Step 0 (General) content: the identity-name field is shown.
        cut.Markup.ShouldContain("Identity name");
    }

    [Fact]
    public void EditModeDisablesIdentityInput()
    {
        Swap();
        var cut = RenderWizard(p => p.Add(x => x.Name, "Customers"));
        // Why: `_isEdit` is driven purely by the Name parameter; the identity input is
        // disabled in edit mode. (The header name only appears after the provider loads
        // the existing model, which the stub deliberately skips, so it is not asserted.)
        cut.Find("input").HasAttribute("disabled").ShouldBeTrue();
    }

    // ── Step 0 (General) — KNOWN-BUG PROBE ──────────────────────────────────

    [Fact]
    public void Step0RendersWithoutThrowingOptionPickerCategoryBranch()
    {
        // Why: this is the suspected live-crash branch (OptionPicker<IDataSetCategory>
        // with StaticOptions = DataSetCategories.All()). Rendering must not throw.
        Swap();
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Classification");
        cut.Markup.ShouldContain("Identity name");
    }

    [Fact]
    public void Step0ServiceOptionSelectUsesDataSetTypesWhenPresent()
    {
        Swap(new DataSetWizardContext { DataSetTypes = [Type("Federated", "Federated DS"), Type("Compound")] });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Federated DS");
        cut.Markup.ShouldContain("Compound");
    }

    [Fact]
    public void Step0ServiceOptionSelectShowsNoFabricatedTypesWhenNoneLoaded()
    {
        // Why: the strategy options come exclusively from the DataSetTypes TypeCollection. An empty
        // set means types failed to load — the wizard must surface that, NOT fabricate a hardcoded
        // list (which previously offered the deleted "Standard" type). No-fallback rule.
        Swap(new DataSetWizardContext { DataSetTypes = [] });
        var cut = RenderWizard();
        cut.Markup.ShouldNotContain("Standard");
        cut.Markup.ShouldContain("could not load");
    }

    // ── Step 1 (Fields) ─────────────────────────────────────────────────────

    [Fact]
    public void Step1AddFieldButtonAddsRowToFieldsTable()
    {
        Swap();
        var cut = RenderWizard();
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Field", StringComparison.Ordinal)).Click();
        // one field row -> the default field name FLD_1 appears as the input value
        cut.Markup.ShouldContain("FLD_1");
    }

    [Fact]
    public void Step1FieldTypeUsesCapabilityFieldTypesWhenPresent()
    {
        Swap(new DataSetWizardContext
        {
            SelectedDataStoreCapabilities = new ConnectionTypeCapabilitiesPayload
            {
                FieldTypes = [new FieldTypeInfoPayload { Name = "MoneyType", DisplayName = "Money" }]
            }
        });
        var cut = RenderWizard();
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Field", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Money");
    }

    [Fact]
    public void Step1FieldTypeFallsBackToHardcodedTypesWhenNoCapabilities()
    {
        Swap(new DataSetWizardContext { SelectedDataStoreCapabilities = null });
        var cut = RenderWizard();
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Field", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Boolean");
        cut.Markup.ShouldContain("Guid");
    }

    [Fact]
    public async Task Step1ToggleImportPanelOpensPanelAndLoadsStoresWhenEmpty()
    {
        var loadCalls = 0;
        Swap(new DataSetWizardContext
        {
            DataStores = [],
            OnLoadDataStores = () => { loadCalls++; return Task.CompletedTask; }
        });
        var cut = RenderWizard();
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Import From Table", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Container Import");
        loadCalls.ShouldBe(1);
    }

    [Fact]
    public void Step1ImportPanelShowsContainerSpinnerWhenBusy()
    {
        Swap(new DataSetWizardContext
        {
            DataStores = [new DataStoreSummaryPayload { Name = "Sql1" }],
            IsContainerBusy = true
        });
        var cut = RenderWizard();
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Import From Table", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Loading containers...");
    }

    // ── Step 2 (Sources) ────────────────────────────────────────────────────

    [Fact]
    public void Step2AddSourceButtonAddsSourceBlock()
    {
        Swap(new DataSetWizardContext { DataStores = [new DataStoreSummaryPayload { Name = "Sql1" }] });
        var cut = RenderWizard();
        Advance(cut, 2);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Node ID");
        // the default source name SRC_1 appears in the new block header + input value
        cut.Markup.ShouldContain("SRC_1");
    }

    [Fact]
    public void Step2ProtocolSelectUsesDataStoreTypesWhenPresent()
    {
        Swap(new DataSetWizardContext { DataStoreTypes = [Type("PostgreSql", "Postgres")] });
        var cut = RenderWizard();
        Advance(cut, 2);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Postgres");
    }

    [Fact]
    public void Step2ProtocolSelectFallsBackToHardcodedWhenNoTypes()
    {
        Swap(new DataSetWizardContext { DataStoreTypes = [] });
        var cut = RenderWizard();
        Advance(cut, 2);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("MsSql");
        cut.Markup.ShouldContain("File");
    }

    // ── Step 3 (Mappings) ───────────────────────────────────────────────────

    [Fact]
    public void Step3RendersMappingsHeaderForSyncedSources()
    {
        Swap(new DataSetWizardContext { DataStores = [new DataStoreSummaryPayload { Name = "Sql1" }] });
        var cut = RenderWizard();
        // add a field + a source so SyncMappings produces rows
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Field", StringComparison.Ordinal)).Click();
        Advance(cut, 1);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).Click();
        Advance(cut, 1); // -> step 3
        cut.Markup.ShouldContain("Logical Target");
        cut.Markup.ShouldContain("Physical Source");
    }

    // ── Step 4 (Advanced) ───────────────────────────────────────────────────

    [Fact]
    public void Step4ShowsMultipleSourcesRequiredWhenFewerThanTwoSources()
    {
        Swap();
        var cut = RenderWizard();
        Advance(cut, 4);
        cut.Markup.ShouldContain("Multiple sources required for linkage.");
    }

    [Fact]
    public void Step4ShowsJoinLinkageWhenTwoOrMoreSources()
    {
        Swap(new DataSetWizardContext { DataStores = [new DataStoreSummaryPayload { Name = "Sql1" }] });
        var cut = RenderWizard();
        Advance(cut, 2);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).Click();
        Advance(cut, 2); // -> step 4
        cut.FindAll("button").Any(b => b.TextContent.Contains("New Link", StringComparison.Ordinal)).ShouldBeTrue();
    }

    // ── Step 5 (Finalize) + submit branches ─────────────────────────────────

    [Fact]
    public void Step5RendersFinalizeSummaryAndCreateButton()
    {
        Swap();
        var cut = RenderWizard();
        Advance(cut, 5);
        cut.Markup.ShouldContain("Ready for initialization");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Create DataSet", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task Step5SubmitSuccessNavigatesToDataSets()
    {
        Swap(new DataSetWizardContext
        {
            OnSubmit = _ => Task.FromResult<DataSetDetailPayload?>(new DataSetDetailPayload { Name = "X" })
        });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = RenderWizard();
        Advance(cut, 5);
        cut.FindAll("button").First(b => b.TextContent.Contains("Create DataSet", StringComparison.Ordinal)).Click();
        await Task.Yield();
        nav.Uri.ShouldEndWith("/datasets");
    }

    [Fact]
    public async Task Step5SubmitNullResultShowsSubmitFailedError()
    {
        Swap(new DataSetWizardContext
        {
            OnSubmit = _ => Task.FromResult<DataSetDetailPayload?>(null)
        });
        var cut = RenderWizard();
        Advance(cut, 5);
        cut.FindAll("button").First(b => b.TextContent.Contains("Create DataSet", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Submit failed");
    }

    [Fact]
    public async Task Step5SubmitThrowsShowsExceptionMessage()
    {
        Swap(new DataSetWizardContext
        {
            OnSubmit = _ => throw new InvalidOperationException("boom-submit")
        });
        var cut = RenderWizard();
        Advance(cut, 5);
        cut.FindAll("button").First(b => b.TextContent.Contains("Create DataSet", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("boom-submit");
    }

    // ── Navigation between steps ────────────────────────────────────────────

    [Fact]
    public void BackButtonHiddenOnStep0VisibleAfterAdvancing()
    {
        Swap();
        var cut = RenderWizard();
        cut.FindAll("button").Any(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).ShouldBeFalse();
        Advance(cut, 1);
        cut.FindAll("button").Any(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void BackButtonDecrementsStep()
    {
        Swap();
        var cut = RenderWizard();
        Advance(cut, 2);
        // Step index 2 (Sources) shows the physical-storage header + Add Source action.
        cut.Markup.ShouldContain("Physical Storage Nodes");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Add Source", StringComparison.Ordinal)).ShouldBeTrue();
        cut.FindAll("button").First(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).Click();
        // back to step 1 (Fields) -> the Logical Schema header + Add Field action reappear
        cut.Markup.ShouldContain("Logical Schema");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Add Field", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void CancelButtonNavigatesToDataSets()
    {
        Swap();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Cancel", StringComparison.Ordinal)).Click();
        nav.Uri.ShouldEndWith("/datasets");
    }

    public void Dispose() => _ctx.Dispose();
}
