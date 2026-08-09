using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Clients.Models;
using Fdw.Services.SecretManagers.Components.SecretManagers;
using Fdw.UI.Components.Blazor.Tests.ConnInfra;
using SecretManagersPage = Fdw.UI.Pages.SecretManagers.Pages.SecretManagersPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.SecretManagers;

/// <summary>
/// Tests for the <see cref="SecretManagersPage"/> FDW page. Relocated from reference-ui's
/// SecretManagersPageTests: the deep loading/empty/error/table/create/edit/delete/detail assertions
/// were reframed in the app to a host smoke, and the equivalent (or stronger) coverage now runs here
/// against the FDW page rendered through a stubbed <see cref="SecretManagerProvider"/>. Assertions
/// were updated to the CURRENT page markup (manager TABLE rows with "Add manager"/"Edit"/"Delete"
/// buttons and a badge loading indicator) — the reference-ui assertions targeted stale card markup.
/// </summary>
[Trait("Category", "Ui")]
public sealed class SecretManagersPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(SecretManagerContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<SecretManagerProvider, SecretManagerContext>(seed));

    private static SecretManagerSummaryPayload Sm(string name, string? type = "EnvironmentVariable", string? desc = null) =>
        new() { Name = name, SecretManagerType = type, Description = desc };

    private static SecretManagerTypeSummaryPayload Type(string name) => new() { Name = name };

    [Fact]
    public void RendersLoadingBadgeWhenLoadingAndEmpty()
    {
        Swap(new SecretManagerContext { IsLoading = true });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll(".badge.b-run").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersEmptyStateWhenNoManagers()
    {
        Swap(new SecretManagerContext());
        var cut = _ctx.Render<SecretManagersPage>();
        cut.Markup.ShouldContain("No secret managers configured");
    }

    [Fact]
    public void RendersErrorBannerWhenError()
    {
        Swap(new SecretManagerContext { LastResult = GenericResult.Failure(new GenericMessage("vault down")) });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.Markup.ShouldContain("vault down");
    }

    [Fact]
    public void RendersTableRowsWithTypePillAndDescription()
    {
        Swap(new SecretManagerContext { SecretManagers = [Sm("Vault1", "AzureKeyVault", "prod vault")] });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.Markup.ShouldContain("Vault1");
        cut.Markup.ShouldContain("AzureKeyVault");
        cut.Markup.ShouldContain("prod vault");
        cut.FindAll("tbody tr").Count.ShouldBe(1);
    }

    [Fact]
    public void RowShowsNoDescriptionFallbackWhenNull()
    {
        Swap(new SecretManagerContext { SecretManagers = [Sm("Vault1", "MsSql", null)] });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.Markup.ShouldContain("No description");
    }

    [Fact]
    public void RowShowsUnknownTypeWhenTypeNull()
    {
        Swap(new SecretManagerContext { SecretManagers = [Sm("Vault1", null)] });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.Markup.ShouldContain("Unknown");
    }

    [Fact]
    public void AddManagerButtonRevealsCreateForm()
    {
        Swap(new SecretManagerContext { AvailableTypes = [Type("AzureKeyVault"), Type("MsSql")] });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add manager", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("New Secret Manager");
        cut.Find("textarea").ShouldNotBeNull();
        cut.FindAll("option").Any(o => o.TextContent.Contains("AzureKeyVault", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void CreateFormCancelHidesForm()
    {
        Swap(new SecretManagerContext());
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add manager", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.FindAll("input").Any(i => string.Equals(i.GetAttribute("placeholder"), "e.g. PROD_VAULT", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateFormMissingFieldsDoesNotInvokeCreate()
    {
        var created = false;
        Swap(new SecretManagerContext { OnCreate = _ => { created = true; return Task.FromResult<IGenericResult>(GenericResult.Success()); } });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add manager", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        created.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateFormFilledInvokesCreate()
    {
        CreateSecretManagerPayload? captured = null;
        Swap(new SecretManagerContext
        {
            AvailableTypes = [Type("MsSql")],
            OnCreate = r => { captured = r; return Task.FromResult<IGenericResult>(GenericResult.Success()); }
        });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add manager", StringComparison.Ordinal)).Click();
        cut.Find("input[placeholder='e.g. PROD_VAULT']").Change("MYVAULT");
        cut.Find("select").Change("MsSql");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        captured.ShouldNotBeNull();
        captured!.Name.ShouldBe("MYVAULT");
        captured.SecretManagerType.ShouldBe("MsSql");
    }

    [Fact]
    public async Task DeleteButtonInvokesOnDelete()
    {
        string? deleted = null;
        Swap(new SecretManagerContext
        {
            SecretManagers = [Sm("Vault1")],
            OnDelete = n => { deleted = n; return Task.FromResult<IGenericResult>(GenericResult.Success()); }
        });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Delete", StringComparison.Ordinal)).Click();
        await Task.Yield();
        deleted.ShouldBe("Vault1");
    }

    [Fact]
    public void EditButtonOpensEditDialog()
    {
        Swap(new SecretManagerContext { SecretManagers = [Sm("Vault1", "MsSql", "old desc")] });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Edit", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Edit: Vault1");
    }

    [Fact]
    public async Task EditDialogSaveInvokesOnUpdate()
    {
        string? updatedName = null;
        UpdateSecretManagerPayload? updReq = null;
        Swap(new SecretManagerContext
        {
            SecretManagers = [Sm("Vault1", "MsSql", "old")],
            OnUpdate = (n, r) => { updatedName = n; updReq = r; return Task.FromResult<IGenericResult>(GenericResult.Success()); }
        });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Edit", StringComparison.Ordinal)).Click();
        cut.Find("textarea").Change("new desc");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Save", StringComparison.Ordinal)).Click();
        await Task.Yield();
        updatedName.ShouldBe("Vault1");
        updReq!.Description.ShouldBe("new desc");
    }

    [Fact]
    public void EditDialogCancelClosesDialog()
    {
        Swap(new SecretManagerContext { SecretManagers = [Sm("Vault1", "MsSql", "d")] });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Edit", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Edit: Vault1");
    }

    [Fact]
    public async Task RowClickInvokesOnSelect()
    {
        string? selected = null;
        Swap(new SecretManagerContext
        {
            SecretManagers = [Sm("Vault1")],
            OnSelect = n => { selected = n; return Task.FromResult<IGenericResult>(GenericResult.Success()); }
        });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.FindAll("tbody tr")[0].Click();
        await Task.Yield();
        selected.ShouldBe("Vault1");
    }

    [Fact]
    public void DetailPanelRendersWhenSelectedManagerSet()
    {
        Swap(new SecretManagerContext
        {
            SecretManagers = [Sm("Vault1")],
            SelectedManager = new SecretManagerDetailPayload
            {
                Name = "Vault1",
                SecretManagerType = "MsSql",
                Description = "the desc",
                ServiceOptionType = "MsSqlSecret"
            }
        });
        var cut = _ctx.Render<SecretManagersPage>();
        cut.Markup.ShouldContain("the desc");
        cut.Markup.ShouldContain("MsSqlSecret");
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Close", StringComparison.Ordinal)).ShouldBeTrue();
    }

    public void Dispose() => _ctx.Dispose();
}
