using Bunit;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Components.ConnectionEditor;
using Fdw.Services.Connections.Components.ConnectionWizard;
using Fdw.Services.Connections.UI.Pages.Pages;
using Fdw.UI.Components.Blazor.Tests.ConnInfra;

namespace Fdw.UI.Components.Blazor.Tests.Components.Connections;

/// <summary>
/// Tests for the <see cref="ConnectionWizard"/> FDW page. Relocated from reference-ui's
/// ConnectionWizardTests: the deep step/field/validation/test/save (create) and edit-mode
/// assertions were reframed in the app to a host smoke, and the equivalent (or stronger) coverage
/// now runs here against the FDW page rendered through stubbed ConnectionWizardProvider /
/// ConnectionEditorProvider components. Assertions were updated to the CURRENT page markup
/// (spaced field labels, .sw toggles, .stepper step indicator, --success/--signal result colors,
/// Continue/Back navigation) — the reference-ui assertions targeted stale underscored markup.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ConnectionWizardPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapWizard(ConnectionWizardContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<ConnectionWizardProvider, ConnectionWizardContext>(seed));

    private void SwapEditor(ConnectionEditorContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<ConnectionEditorProvider, ConnectionEditorContext>(seed));

    private static ConnectionTypePayload Type(string name) => new() { Name = name, Category = name };

    private static TypeCollectionValueSummary Auth(string name, params string[] required) =>
        new() { Name = name, RequiredProperties = required, ExpectedProperties = [] };

    private IRenderedComponent<ConnectionWizard> RenderWizard(string? name = null) =>
        _ctx.Render<ConnectionWizard>(p => { if (name is not null) { p.Add(x => x.Name, name); } });

    // CREATE WIZARD (no Name) ────────────────────────────────────────────────

    [Fact]
    public void WizardStep0RendersConfigureFields()
    {
        SwapWizard(new ConnectionWizardContext { Step = 0, IsFirstStep = true, ConnectionTypes = [Type("MsSql")] });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("New Connection");
        cut.Markup.ShouldContain("Connection Name");
        cut.Markup.ShouldContain("Configure"); // step title
        cut.FindAll(".stepper").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void WizardStep0LoadingTypesShowsLoadingHint()
    {
        SwapWizard(new ConnectionWizardContext { Step = 0, ConnectionTypes = [] });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Loading connection types...");
    }

    [Fact]
    public void WizardStep0HttpTypeRendersBaseUrlNotServerPort()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("HttpRest")],
            ConnectionConfig = new CreateConnectionClientRequest { ServiceType = "HttpRest" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Base URL");
        cut.Markup.ShouldNotContain("Server Host");
    }

    [Fact]
    public void WizardStep0SqlTypeRendersServerPortDatabase()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            ConnectionConfig = new CreateConnectionClientRequest { ServiceType = "MsSql" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Server Host");
        cut.Markup.ShouldContain("Port");
        cut.Markup.ShouldContain("Database");
    }

    [Fact]
    public void WizardStep0PostgreSqlZeroPortShowsDefaultHint()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("PostgreSql")],
            ConnectionConfig = new CreateConnectionClientRequest { ServiceType = "PostgreSql", Port = 0 }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Default: 5432");
    }

    [Fact]
    public void WizardStep0AuthTypesRenderSelect()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            ConnectionConfig = new CreateConnectionClientRequest { ServiceType = "MsSql" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Auth Type");
        cut.Markup.ShouldContain("SqlLogin");
    }

    [Fact]
    public void WizardStep0UsernameFieldRendersWhenAuthRequiresUsername()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            ConnectionConfig = new CreateConnectionClientRequest
            {
                ServiceType = "MsSql",
                AuthenticationType = "SqlLogin",
                Authentication = new ConnectionAuthenticationRequest()
            }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Username");
    }

    [Fact]
    public void WizardStep0SecretManagerSelectRendersWhenManagersAvailable()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            AvailableSecretManagers = ["PROD_VAULT"],
            ConnectionConfig = new CreateConnectionClientRequest
            {
                ServiceType = "MsSql",
                AuthenticationType = "SqlLogin",
                Authentication = new ConnectionAuthenticationRequest()
            }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Secret Manager");
        cut.Markup.ShouldContain("PROD_VAULT");
    }

    [Fact]
    public void WizardStep0SecretModeNewShowsPasswordInput()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            AvailableSecretManagers = ["PROD_VAULT"],
            SelectedSecretManagerName = "PROD_VAULT",
            SecretStorageMode = "new",
            ConnectionConfig = new CreateConnectionClientRequest
            {
                ServiceType = "MsSql",
                AuthenticationType = "SqlLogin",
                Authentication = new ConnectionAuthenticationRequest()
            }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Store New Password");
        cut.FindAll("input[type=password]").Count.ShouldBe(1);
    }

    [Fact]
    public void WizardStep0SecretModeNewAlreadyStoredShowsStoredBadge()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            AvailableSecretManagers = ["PROD_VAULT"],
            SelectedSecretManagerName = "PROD_VAULT",
            SecretStorageMode = "new",
            StoredSecretKeyName = "conn-prod-pw",
            ConnectionConfig = new CreateConnectionClientRequest
            {
                ServiceType = "MsSql",
                AuthenticationType = "SqlLogin",
                Authentication = new ConnectionAuthenticationRequest()
            }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("stored");
        cut.Markup.ShouldContain("conn-prod-pw");
    }

    [Fact]
    public void WizardStep0SecretModeExistingShowsKeyNameInput()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            AvailableSecretManagers = ["PROD_VAULT"],
            SelectedSecretManagerName = "PROD_VAULT",
            SecretStorageMode = "existing",
            ConnectionConfig = new CreateConnectionClientRequest
            {
                ServiceType = "MsSql",
                AuthenticationType = "SqlLogin",
                Authentication = new ConnectionAuthenticationRequest()
            }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Secret Key Name");
        cut.Markup.ShouldContain("Use Existing Key");
    }

    [Fact]
    public void WizardStep0NoSecretManagersShowsPlainKeyNameInput()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            AvailableSecretManagers = [],
            ConnectionConfig = new CreateConnectionClientRequest
            {
                ServiceType = "MsSql",
                AuthenticationType = "SqlLogin",
                Authentication = new ConnectionAuthenticationRequest()
            }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Secret Key Name");
        cut.Markup.ShouldContain("resolved at runtime");
    }

    [Fact]
    public void WizardErrorMessageRendersBanner()
    {
        SwapWizard(new ConnectionWizardContext { Step = 0, ErrorMessage = "boom error" });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("boom error");
    }

    [Fact]
    public async Task WizardStep0NameInputBindsToModel()
    {
        var ctx = new ConnectionWizardContext { Step = 0, IsFirstStep = true, ConnectionTypes = [Type("MsSql")] };
        SwapWizard(ctx);
        var cut = RenderWizard();
        cut.Find("input[placeholder='e.g. PROD_MSSQL']").Change("MYCONN");
        await Task.Yield();
        ctx.ConnectionConfig.Name.ShouldBe("MYCONN");
    }

    [Fact]
    public async Task WizardStep0TypeSelectInvokesOnServiceTypeChanged()
    {
        string? changed = null;
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql"), Type("Http")],
            OnServiceTypeChanged = t => { changed = t; return Task.CompletedTask; }
        });
        var cut = RenderWizard();
        cut.FindAll("select").First(s => s.InnerHtml.Contains("Select Type", StringComparison.Ordinal)).Change("Http");
        await Task.Yield();
        changed.ShouldBe("Http");
    }

    [Fact]
    public async Task WizardStep0EncryptToggleFlipsModel()
    {
        var ctx = new ConnectionWizardContext
        {
            Step = 0,
            ConnectionTypes = [Type("MsSql")],
            ConnectionConfig = new CreateConnectionClientRequest { ServiceType = "MsSql", Encrypt = false }
        };
        SwapWizard(ctx);
        var cut = RenderWizard();
        // The Encrypt toggle is the first .sw switch on the page.
        cut.FindAll("span.sw")[0].Click();
        await Task.Yield();
        ctx.ConnectionConfig.Encrypt.ShouldBeTrue();
    }

    [Fact]
    public void WizardNextButtonDisabledWhenCannotAdvance()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            IsFirstStep = true,
            ConnectionTypes = [Type("MsSql")],
            ConnectionConfig = new CreateConnectionClientRequest { Name = "", ServiceType = "" }
        });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void WizardNextButtonEnabledWhenNameAndTypeSet()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            IsFirstStep = true,
            ConnectionTypes = [Type("MsSql")],
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1", ServiceType = "MsSql" }
        });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task WizardNextButtonInvokesOnNextStep()
    {
        var advanced = false;
        SwapWizard(new ConnectionWizardContext
        {
            Step = 0,
            IsFirstStep = true,
            ConnectionTypes = [Type("MsSql")],
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1", ServiceType = "MsSql" },
            OnNextStep = () => { advanced = true; return Task.CompletedTask; }
        });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal)).Click();
        await Task.Yield();
        advanced.ShouldBeTrue();
    }

    [Fact]
    public void WizardNoPreviousButtonOnFirstStep()
    {
        SwapWizard(new ConnectionWizardContext { Step = 0, IsFirstStep = true, ConnectionTypes = [Type("MsSql")] });
        var cut = RenderWizard();
        cut.FindAll("button").Any(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).ShouldBeFalse();
    }

    // Step 1: Test ──────────────────────────────────────────────────────────────

    [Fact]
    public void WizardStep1RendersTestSummaryAndRunTestButton()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 1,
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1", ServiceType = "MsSql", Server = "s", Database = "d" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Test Connection"); // step title
        cut.Markup.ShouldContain("Run Test");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).ShouldBeTrue(); // not first step
    }

    [Fact]
    public void WizardStep1LoadingShowsTestingSpinner()
    {
        SwapWizard(new ConnectionWizardContext { Step = 1, IsLoading = true });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Testing connection...");
    }

    [Fact]
    public void WizardStep1TestSuccessShowsGreenResult()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 1,
            TestResult = new TestConnectionClientResponse { Success = true, Message = "Connected!" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Connected!");
        cut.Markup.ShouldContain("--success");
    }

    [Fact]
    public void WizardStep1TestFailureShowsRedResult()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 1,
            TestResult = new TestConnectionClientResponse { Success = false, Message = "No route" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("No route");
        cut.Markup.ShouldContain("--signal");
    }

    [Fact]
    public async Task WizardStep1RunTestButtonInvokesOnTestConnection()
    {
        var ran = false;
        SwapWizard(new ConnectionWizardContext
        {
            Step = 1,
            OnTestConnection = () => { ran = true; return Task.CompletedTask; }
        });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Run Test", StringComparison.Ordinal)).Click();
        await Task.Yield();
        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task WizardPreviousButtonInvokesOnPreviousStep()
    {
        var back = false;
        SwapWizard(new ConnectionWizardContext { Step = 1, OnPreviousStep = () => back = true });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Back", StringComparison.Ordinal)).Click();
        await Task.Yield();
        back.ShouldBeTrue();
    }

    // Step 2: Save ──────────────────────────────────────────────────────────────

    [Fact]
    public void WizardStep2RendersReviewSummaryAndCreateButton()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 2,
            IsLastStep = true,
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1", ServiceType = "MsSql" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Review &amp; Save"); // step title (HTML-encoded ampersand)
        cut.FindAll("button").Any(b => b.TextContent.Contains("Create Connection", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void WizardStep2TestPassedShowsPassedBadge()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 2,
            IsLastStep = true,
            TestResult = new TestConnectionClientResponse { Success = true, Message = "ok" },
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Passed");
    }

    [Fact]
    public void WizardStep2TestFailedShowsSavingAnywayWarning()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 2,
            IsLastStep = true,
            TestResult = new TestConnectionClientResponse { Success = false, Message = "x" },
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1" }
        });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("saving anyway");
    }

    [Fact]
    public async Task WizardStep2CreateButtonInvokesOnSaveConnection()
    {
        var saved = false;
        SwapWizard(new ConnectionWizardContext
        {
            Step = 2,
            IsLastStep = true,
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1" },
            OnSaveConnection = () => { saved = true; return Task.CompletedTask; }
        });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Create Connection", StringComparison.Ordinal)).Click();
        await Task.Yield();
        saved.ShouldBeTrue();
    }

    [Fact]
    public void WizardStep2SavingDisablesCreateButtonAndShowsSavingText()
    {
        SwapWizard(new ConnectionWizardContext
        {
            Step = 2,
            IsLastStep = true,
            IsLoading = true,
            ConnectionConfig = new CreateConnectionClientRequest { Name = "C1" }
        });
        var cut = RenderWizard();
        cut.FindAll("button").First(b => b.TextContent.Contains("Saving...", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void WizardIsCompleteShowsSuccessPanel()
    {
        SwapWizard(new ConnectionWizardContext { IsComplete = true });
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Connection created successfully");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Back to Connections", StringComparison.Ordinal)).ShouldBeTrue();
    }

    // EDIT MODE (Name set) ───────────────────────────────────────────────────────

    [Fact]
    public void EditRendersReadonlyNameAndType()
    {
        SwapEditor(new ConnectionEditorContext
        {
            Model = new CreateConnectionClientRequest { Name = "PROD", ServiceType = "MsSql" }
        });
        var cut = RenderWizard("PROD");
        cut.Markup.ShouldContain("Edit Connection");
        cut.FindAll("input[disabled]").Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void EditLoadingNoTypesShowsLoadingBadge()
    {
        SwapEditor(new ConnectionEditorContext { IsLoading = true, ConnectionTypes = [] });
        var cut = RenderWizard("PROD");
        cut.FindAll(".badge.b-run").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void EditErrorMessageRendersBanner()
    {
        SwapEditor(new ConnectionEditorContext
        {
            ConnectionTypes = [Type("MsSql")],
            ErrorMessage = "edit failed",
            Model = new CreateConnectionClientRequest { Name = "P" }
        });
        var cut = RenderWizard("PROD");
        cut.Markup.ShouldContain("edit failed");
    }

    [Fact]
    public void EditAuthTypeRequiresUsernameRendersUsername()
    {
        SwapEditor(new ConnectionEditorContext
        {
            ConnectionTypes = [Type("MsSql")],
            AuthenticationTypes = [Auth("SqlLogin", "Username")],
            AuthTypeRequiresProperty = prop => string.Equals(prop, "Username", StringComparison.OrdinalIgnoreCase),
            Model = new CreateConnectionClientRequest { Name = "P", Authentication = new ConnectionAuthenticationRequest() }
        });
        var cut = RenderWizard("PROD");
        cut.Markup.ShouldContain("Username");
    }

    [Fact]
    public void EditAuthTypeRequiresSecretKeyNameRendersSecretKey()
    {
        SwapEditor(new ConnectionEditorContext
        {
            ConnectionTypes = [Type("MsSql")],
            AuthTypeRequiresProperty = prop => string.Equals(prop, "SecretKeyName", StringComparison.OrdinalIgnoreCase),
            Model = new CreateConnectionClientRequest { Name = "P", Authentication = new ConnectionAuthenticationRequest() }
        });
        var cut = RenderWizard("PROD");
        cut.Markup.ShouldContain("Secret Key Name");
    }

    [Fact]
    public async Task EditSaveButtonInvokesOnSubmit()
    {
        var submitted = false;
        SwapEditor(new ConnectionEditorContext
        {
            ConnectionTypes = [Type("MsSql")],
            Model = new CreateConnectionClientRequest { Name = "P", ServiceType = "MsSql" },
            OnSubmit = () => { submitted = true; return Task.CompletedTask; }
        });
        var cut = RenderWizard("PROD");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Changes", StringComparison.Ordinal)).Click();
        await Task.Yield();
        submitted.ShouldBeTrue();
    }

    [Fact]
    public async Task EditActiveToggleFlipsOverride()
    {
        SwapEditor(new ConnectionEditorContext
        {
            ConnectionTypes = [Type("MsSql")],
            IsActive = true,
            Model = new CreateConnectionClientRequest { Name = "P", ServiceType = "MsSql" }
        });
        var cut = RenderWizard("PROD");
        // Three .sw toggles in edit mode: Encrypt, TrustServerCertificate, Active — Active is the 3rd.
        var toggles = cut.FindAll("span.sw");
        toggles.Count.ShouldBe(3);
        toggles[2].Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Active");
    }

    public void Dispose() => _ctx.Dispose();
}
