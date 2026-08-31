using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Clients;
using Fdw.Services.Authorization.Clients.Models;
using Fdw.Services.Connections.Clients;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Clients;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Clients;
using Fdw.Services.Multitenancy.Clients;
using Fdw.Services.Multitenancy.Clients.Models;
using Fdw.Services.Notifications.Clients;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Quality.Clients;
using Fdw.Services.Quality.Clients.Models;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.SecretManagers.Clients;
using Fdw.Services.SecretManagers.Clients.Models;
using Fdw.Services.Settings.Clients;
using Fdw.Services.Settings.Clients.Models;
using Fdw.Services.Users.Clients;
using Fdw.Services.Users.Clients.Models;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Components.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Menu screen for configuration management options.
/// </summary>
public sealed class ConfigurationMenuScreen : ScreenBase
{
    private const string ColumnName = "name";
    private const string ColumnType = "type";
    private const string ColumnLastTest = "lastTest";
    private const string ColumnCreated = "created";
    private const string ColumnConnection = "connection";
    private const string ColumnLastDiscovered = "lastDiscovered";
    private const string ColumnCategory = "category";
    private const string ColumnFields = "fields";
    private const string ColumnDescription = "description";
    private const string ColumnEnabled = "enabled";
    private const string ColumnSeverity = "severity";
    private const string ColumnService = "service";
    private const string ColumnPipeline = "pipeline";
    private const string ColumnTrigger = "trigger";
    private const string ColumnNextRun = "nextRun";
    private const string ColumnValue = "value";
    private const string ColumnActive = "active";
    private const string ColumnEmail = "email";
    private const string ColumnRoles = "roles";
    private const string ColumnLastLogin = "lastLogin";
    private const string ColumnDisplayName = "displayName";
    private const string ColumnScope = "scope";
    private const string ColumnSlug = "slug";
    private const string ColumnOrdinal = "ordinal";

    private readonly IConnectionManager _connectionManager;
    private readonly ConnectionApiClient _connectionApiClient;
    private readonly DataStoreApiClient _dataStoreApiClient;
    private readonly DataSetApiClient _dataSetApiClient;
    private readonly IPipelineClient _pipelineClient;
    private readonly SecretManagerApiClient _secretManagerApiClient;
    private readonly NotificationApiClient _notificationApiClient;
    private readonly IScheduleClient _scheduleClient;
    private readonly SettingsApiClient _settingsApiClient;
    private readonly UserApiClient _userApiClient;
    private readonly RoleApiClient _roleApiClient;
    private readonly TenantApiClient _tenantApiClient;
    private readonly QualityApiClient _qualityApiClient;
    private readonly ProjectApiClient _projectApiClient;
    private readonly NodeApiClient _nodeApiClient;
    private readonly IUIRenderer _renderer;
    private readonly IRenderContext _renderContext;

    /// <inheritdoc />
    public override string Title => "Configuration Management";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationMenuScreen"/> class.
    /// </summary>
    public ConfigurationMenuScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        IConnectionManager connectionManager,
        ConnectionApiClient connectionApiClient,
        DataStoreApiClient dataStoreApiClient,
        DataSetApiClient dataSetApiClient,
        IPipelineClient pipelineClient,
        SecretManagerApiClient secretManagerApiClient,
        NotificationApiClient notificationApiClient,
        IScheduleClient scheduleClient,
        SettingsApiClient settingsApiClient,
        UserApiClient userApiClient,
        RoleApiClient roleApiClient,
        TenantApiClient tenantApiClient,
        QualityApiClient qualityApiClient,
        ProjectApiClient projectApiClient,
        NodeApiClient nodeApiClient,
        IUIRenderer renderer,
        IRenderContext renderContext)
        : base(console, theme, screenFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(connectionApiClient);
        ArgumentNullException.ThrowIfNull(dataStoreApiClient);
        ArgumentNullException.ThrowIfNull(dataSetApiClient);
        ArgumentNullException.ThrowIfNull(pipelineClient);
        ArgumentNullException.ThrowIfNull(secretManagerApiClient);
        ArgumentNullException.ThrowIfNull(notificationApiClient);
        ArgumentNullException.ThrowIfNull(scheduleClient);
        ArgumentNullException.ThrowIfNull(settingsApiClient);
        ArgumentNullException.ThrowIfNull(userApiClient);
        ArgumentNullException.ThrowIfNull(roleApiClient);
        ArgumentNullException.ThrowIfNull(tenantApiClient);
        ArgumentNullException.ThrowIfNull(qualityApiClient);
        ArgumentNullException.ThrowIfNull(projectApiClient);
        ArgumentNullException.ThrowIfNull(nodeApiClient);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(renderContext);

        _connectionManager = connectionManager;
        _connectionApiClient = connectionApiClient;
        _dataStoreApiClient = dataStoreApiClient;
        _dataSetApiClient = dataSetApiClient;
        _pipelineClient = pipelineClient;
        _secretManagerApiClient = secretManagerApiClient;
        _notificationApiClient = notificationApiClient;
        _scheduleClient = scheduleClient;
        _settingsApiClient = settingsApiClient;
        _userApiClient = userApiClient;
        _roleApiClient = roleApiClient;
        _tenantApiClient = tenantApiClient;
        _qualityApiClient = qualityApiClient;
        _projectApiClient = projectApiClient;
        _nodeApiClient = nodeApiClient;
        _renderer = renderer;
        _renderContext = renderContext;
    }

    /// <inheritdoc />
    // MA0051: Method length acceptable - procedural menu rendering with connection check, table display, and prompt
#pragma warning disable MA0051 // Method is too long
    public override async Task<NavigationResult> Show()
#pragma warning restore MA0051
    {
        RenderHeader();

        // Check connection status
        var status = _connectionManager.GetStatus();
        if (!status.IsConnected)
        {
            RenderWarning("Not connected to any instance. Please connect first.");
            Console.WriteLine();

            var connectChoice = Console.Confirm($"[{Theme.Colors.Primary}]Go to connections?[/]", true);
            if (connectChoice)
            {
                return NavigationResult.Push(ScreenFactory.Create<ConnectionsScreen>());
            }
            return NavigationResult.Pop();
        }

        Console.MarkupLine($"[{Theme.Colors.Muted}]Connected to: {Markup.Escape(status.InstanceName ?? "Unknown")}[/]");
        Console.WriteLine();

        var choices = BuildAreas();

        // Render choices as a table for better visibility
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Theme.Colors.Muted)
            .HideHeaders();

        table.AddColumn(new TableColumn("").LeftAligned().Width(20));
        table.AddColumn(new TableColumn("").LeftAligned());

        foreach (var choice in choices)
        {
            if (string.Equals(choice.Id, "back", StringComparison.Ordinal))
            {
                continue;
            }
            table.AddRow(
                $"[{Theme.Colors.Primary}]{choice.Label}[/]",
                $"[{Theme.Colors.Muted}]{choice.Description}[/]"
            );
        }

        Console.Write(table);
        Console.WriteLine();

        var prompt = new SelectionPrompt<(string Id, string Label, string Description, Func<Task>? Load)>()
            .Title($"[{Theme.Colors.Primary}]Select configuration area:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);

        return await HandleSelection(selected).ConfigureAwait(false);
    }

    /// <summary>
    /// The configuration areas this screen offers, described once — label, blurb, and the loader
    /// that paints the area.
    /// </summary>
    /// <remarks>
    /// Why: each area used to appear in three places (the choices list, a switch case in
    /// <see cref="HandleSelection"/>, and its own Show method), so adding one meant editing three
    /// sites and grew the switch until it tripped the FDW007 complexity gate. Carrying the loader
    /// alongside the label makes an area a single list entry and reduces dispatch to a delegate
    /// call — the gate was pointing at a real design problem, not an arbitrary threshold.
    /// A null <c>Load</c> marks the terminal "back" entry.
    /// </remarks>
    private List<(string Id, string Label, string Description, Func<Task>? Load)> BuildAreas() =>
        new List<(string Id, string Label, string Description, Func<Task>? Load)>
        {
            ("connections", "Database Connections", "Manage SQL Server, REST, and HTTP connections", ShowConnectionsList),
            ("datasets", "DataSets", "Configure data mappings and field definitions", ShowDataSetsList),
            ("datastores", "DataStores", "Manage data storage configurations", ShowDataStoresList),
            ("pipelines", "Pipelines", "Configure ETL pipeline stages", ShowPipelinesList),
            ("etlprojects", "ETL Projects", "Orchestration project definitions", ShowEtlProjectsList),
            ("etlnodes", "ETL Nodes", "Root orchestration nodes and their ordering", ShowEtlNodesList),
            ("schedules", "Schedules", "Cron, interval, and one-off pipeline triggers", ShowSchedulesList),
            ("notifications", "Notifications", "Configured notification services", ShowNotificationsList),
            ("notificationrules", "Notification Rules", "Rules that decide when a notification fires", ShowNotificationRulesList),
            ("qualityrules", "Quality Rules", "Data-quality rule definitions", ShowQualityRulesList),
            ("secrets", "Secret Managers", "Azure Key Vault and secret storage", ShowSecretManagersList),
            ("serversettings", "Server Settings", "Instance-level configuration values", ShowServerSettingsList),
            ("users", "Users", "User accounts and their assigned roles", ShowUsersList),
            ("roles", "Roles", "Roles and their permission grants", ShowRolesList),
            ("tenants", "Tenants", "Tenant definitions and their available roles", ShowTenantsList),
            ("back", "Back", "Return to main menu", null)
        };

    private static async Task<NavigationResult> HandleSelection(
        (string Id, string Label, string Description, Func<Task>? Load) selected)
    {
        if (selected.Load is null)
        {
            return NavigationResult.Pop();
        }

        await selected.Load().ConfigureAwait(false);
        return NavigationResult.Stay();
    }

    /// <summary>
    /// Fetches the instance's configured connections through the real API client and paints them
    /// through the render-agnostic seam — no hand-rolled console table.
    /// </summary>
    private async Task ShowConnectionsList()
    {
        IReadOnlyList<ConnectionPayload>? connections = null;
        string? error = null;

        await Console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(Theme.Colors.Primary))
            .StartAsync("Loading connections from the instance...", async _ =>
            {
                var result = await _connectionApiClient.GetConnections().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    connections = result.Value;
                }
                else
                {
                    error = result.CurrentMessage;
                }
            }).ConfigureAwait(false);

        if (error is not null)
        {
            RenderStatus(error, isError: true);
            PauseForAcknowledgement();
            return;
        }

        if (connections is null)
        {
            RenderStatus("The instance reported success but returned no connection data.", isError: true);
            PauseForAcknowledgement();
            return;
        }

        var render = await _renderer.RenderListPage(BuildConnectionsPage(connections), _renderContext).ConfigureAwait(false);
        if (!render.Success)
        {
            RenderStatus(render.Error ?? "The connections list could not be rendered.", isError: true);
            PauseForAcknowledgement();
        }
    }

    private static ListPageModel BuildConnectionsPage(IReadOnlyList<ConnectionPayload> connections)
    {
        var page = new ListPageModel
        {
            Id = "config-connections",
            Title = "Database Connections",
            EntityTypeName = "Connection",
            Pagination = new PaginationState { TotalItems = connections.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnType, "Type"));
        page.AddColumn(ListColumnDefinition.Create(ColumnLastTest, "Last Test"));

        var createdColumn = ListColumnDefinition.Create(ColumnCreated, "Created");
        createdColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(createdColumn);

        foreach (var connection in connections)
        {
            var row = new ListRowModel { Id = connection.Name };
            row.SetValue(ColumnName, connection.Name);
            row.SetValue(ColumnType, connection.ConnectionType);
            row.SetValue(ColumnLastTest, DescribeLastTest(connection.LastTestSuccess));
            row.SetValue(
                ColumnCreated,
                connection.CreatedAt.ToString("g", CultureInfo.CurrentCulture));

            row.Status = connection.LastTestSuccess switch
            {
                true => RowStatuses.Success,
                false => RowStatuses.Error,
                null => RowStatuses.Normal,
            };

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's configured DataStores through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowDataStoresList() =>
        LoadAndRenderList<DataStoreSummaryPayload>(
            "DataStore",
            "Loading data stores from the instance...",
            _dataStoreApiClient.GetDataStores,
            BuildDataStoresPage);

    private static ListPageModel BuildDataStoresPage(IReadOnlyList<DataStoreSummaryPayload> dataStores)
    {
        var page = new ListPageModel
        {
            Id = "config-datastores",
            Title = "DataStores",
            EntityTypeName = "DataStore",
            Pagination = new PaginationState { TotalItems = dataStores.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnConnection, "Connection"));
        page.AddColumn(ListColumnDefinition.Create(ColumnLastDiscovered, "Last Discovered"));

        var createdColumn = ListColumnDefinition.Create(ColumnCreated, "Created");
        createdColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(createdColumn);

        foreach (var dataStore in dataStores)
        {
            var row = new ListRowModel { Id = dataStore.Name };
            row.SetValue(ColumnName, dataStore.Name);
            row.SetValue(ColumnConnection, dataStore.ConnectionName);
            row.SetValue(
                ColumnLastDiscovered,
                dataStore.LastDiscoveredAt?.ToString("g", CultureInfo.CurrentCulture) ?? "Never");
            row.SetValue(
                ColumnCreated,
                dataStore.CreatedAt.ToString("g", CultureInfo.CurrentCulture));

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's configured DataSets through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowDataSetsList() =>
        LoadAndRenderList<DataSetSummaryPayload>(
            "DataSet",
            "Loading data sets from the instance...",
            _dataSetApiClient.GetDataSets,
            BuildDataSetsPage);

    private static ListPageModel BuildDataSetsPage(IReadOnlyList<DataSetSummaryPayload> dataSets)
    {
        var page = new ListPageModel
        {
            Id = "config-datasets",
            Title = "DataSets",
            EntityTypeName = "DataSet",
            Pagination = new PaginationState { TotalItems = dataSets.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnCategory, "Category"));

        var fieldsColumn = ListColumnDefinition.Create(ColumnFields, "Fields");
        fieldsColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(fieldsColumn);

        var createdColumn = ListColumnDefinition.Create(ColumnCreated, "Created");
        createdColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(createdColumn);

        foreach (var dataSet in dataSets)
        {
            var row = new ListRowModel { Id = dataSet.Name };
            row.SetValue(ColumnName, dataSet.Name);
            row.SetValue(ColumnCategory, dataSet.Category);
            row.SetValue(ColumnFields, dataSet.FieldCount.ToString(CultureInfo.CurrentCulture));
            row.SetValue(
                ColumnCreated,
                dataSet.CreatedAt.ToString("g", CultureInfo.CurrentCulture));

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's configured pipelines through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowPipelinesList() =>
        LoadAndRenderList<PipelineSummaryResponse>(
            "Pipeline",
            "Loading pipelines from the instance...",
            _pipelineClient.List,
            BuildPipelinesPage);

    private static ListPageModel BuildPipelinesPage(IReadOnlyList<PipelineSummaryResponse> pipelines)
    {
        var page = new ListPageModel
        {
            Id = "config-pipelines",
            Title = "Pipelines",
            EntityTypeName = "Pipeline",
            Pagination = new PaginationState { TotalItems = pipelines.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnType, "Type"));

        foreach (var pipeline in pipelines)
        {
            var row = new ListRowModel { Id = pipeline.Name };
            row.SetValue(ColumnName, pipeline.Name);
            row.SetValue(ColumnType, pipeline.PipelineType);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's configured schedules through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowSchedulesList() =>
        LoadAndRenderList<ScheduleInfoDto>(
            "Schedule",
            "Loading schedules from the instance...",
            _scheduleClient.List,
            BuildSchedulesPage);

    private static ListPageModel BuildSchedulesPage(IReadOnlyList<ScheduleInfoDto> schedules)
    {
        var page = new ListPageModel
        {
            Id = "config-schedules",
            Title = "Schedules",
            EntityTypeName = "Schedule",
            Pagination = new PaginationState { TotalItems = schedules.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnPipeline, "Pipeline"));
        page.AddColumn(ListColumnDefinition.Create(ColumnTrigger, "Trigger"));
        page.AddColumn(ListColumnDefinition.Create(ColumnNextRun, "Next Run"));
        page.AddColumn(ListColumnDefinition.Create(ColumnEnabled, "Enabled"));

        foreach (var schedule in schedules)
        {
            var row = new ListRowModel { Id = schedule.Name };
            row.SetValue(ColumnName, schedule.Name);
            row.SetValue(ColumnPipeline, schedule.PipelineName);
            row.SetValue(ColumnTrigger, schedule.SchedulerType);
            row.SetValue(
                ColumnNextRun,
                schedule.NextRunTime?.ToString("g", CultureInfo.CurrentCulture) ?? "—");
            row.SetValue(ColumnEnabled, DescribeEnabled(schedule.IsEnabled));
            row.Status = DescribeEnabledStatus(schedule.IsEnabled);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's configured secret managers through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowSecretManagersList() =>
        LoadAndRenderList<SecretManagerSummaryPayload>(
            "SecretManager",
            "Loading secret managers from the instance...",
            _secretManagerApiClient.GetSecretManagers,
            BuildSecretManagersPage);

    private static ListPageModel BuildSecretManagersPage(IReadOnlyList<SecretManagerSummaryPayload> secretManagers)
    {
        var page = new ListPageModel
        {
            Id = "config-secrets",
            Title = "Secret Managers",
            EntityTypeName = "SecretManager",
            Pagination = new PaginationState { TotalItems = secretManagers.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnType, "Type"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDescription, "Description"));

        foreach (var secretManager in secretManagers)
        {
            var row = new ListRowModel { Id = secretManager.Name };
            row.SetValue(ColumnName, secretManager.Name);
            row.SetValue(ColumnType, secretManager.SecretManagerType);
            row.SetValue(ColumnDescription, secretManager.Description);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's configured notification services through the real API client and paints
    /// them through the render-agnostic seam.
    /// </summary>
    private Task ShowNotificationsList() =>
        LoadAndRenderList<NotificationSummaryDto>(
            "Notification",
            "Loading notifications from the instance...",
            _notificationApiClient.ListNotifications,
            BuildNotificationsPage);

    private static ListPageModel BuildNotificationsPage(IReadOnlyList<NotificationSummaryDto> notifications)
    {
        var page = new ListPageModel
        {
            Id = "config-notifications",
            Title = "Notifications",
            EntityTypeName = "Notification",
            Pagination = new PaginationState { TotalItems = notifications.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnType, "Type"));
        page.AddColumn(ListColumnDefinition.Create(ColumnEnabled, "Enabled"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDescription, "Description"));

        foreach (var notification in notifications)
        {
            var row = new ListRowModel { Id = notification.Name };
            row.SetValue(ColumnName, notification.Name);
            row.SetValue(ColumnType, notification.ServiceOptionType);
            row.SetValue(ColumnEnabled, DescribeEnabled(notification.IsEnabled));
            row.SetValue(ColumnDescription, notification.Description);
            row.Status = DescribeEnabledStatus(notification.IsEnabled);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's notification rules through the real API client and paints them through
    /// the render-agnostic seam.
    /// </summary>
    private Task ShowNotificationRulesList() =>
        LoadAndRenderList<NotificationRuleSummaryDto>(
            "NotificationRule",
            "Loading notification rules from the instance...",
            _notificationApiClient.ListRules,
            BuildNotificationRulesPage);

    private static ListPageModel BuildNotificationRulesPage(IReadOnlyList<NotificationRuleSummaryDto> rules)
    {
        var page = new ListPageModel
        {
            Id = "config-notification-rules",
            Title = "Notification Rules",
            EntityTypeName = "NotificationRule",
            Pagination = new PaginationState { TotalItems = rules.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnSeverity, "Severity"));
        page.AddColumn(ListColumnDefinition.Create(ColumnService, "Service"));
        page.AddColumn(ListColumnDefinition.Create(ColumnEnabled, "Enabled"));

        foreach (var rule in rules)
        {
            var row = new ListRowModel { Id = rule.Name };
            row.SetValue(ColumnName, rule.Name);
            row.SetValue(ColumnSeverity, rule.Severity);
            row.SetValue(ColumnService, rule.NotificationServiceName);
            row.SetValue(ColumnEnabled, DescribeEnabled(rule.IsEnabled));
            row.Status = DescribeEnabledStatus(rule.IsEnabled);

            page.AddRow(row);
        }

        return page;
    }

    private static string DescribeEnabled(bool isEnabled) => isEnabled ? "Yes" : "No";

    private static IRowStatus DescribeEnabledStatus(bool isEnabled) =>
        isEnabled ? RowStatuses.Normal : RowStatuses.Disabled;

    /// <summary>
    /// Fetches the instance's server-level settings through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowServerSettingsList() =>
        LoadAndRenderList<ServerSettingResponse>(
            "ServerSetting",
            "Loading server settings from the instance...",
            _settingsApiClient.List,
            BuildServerSettingsPage);

    private static ListPageModel BuildServerSettingsPage(IReadOnlyList<ServerSettingResponse> settings)
    {
        var page = new ListPageModel
        {
            Id = "config-server-settings",
            Title = "Server Settings",
            EntityTypeName = "ServerSetting",
            Pagination = new PaginationState { TotalItems = settings.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnValue, "Value"));
        page.AddColumn(ListColumnDefinition.Create(ColumnType, "Data Type"));
        page.AddColumn(ListColumnDefinition.Create(ColumnActive, "Active"));

        foreach (var setting in settings)
        {
            var row = new ListRowModel { Id = setting.SettingName };
            row.SetValue(ColumnName, setting.SettingName);
            row.SetValue(ColumnValue, setting.SettingValue);
            row.SetValue(ColumnType, setting.DataType);
            row.SetValue(ColumnActive, DescribeEnabled(setting.IsActive));
            row.Status = DescribeEnabledStatus(setting.IsActive);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's user accounts through the real API client and paints them through the
    /// render-agnostic seam.
    /// </summary>
    private Task ShowUsersList() =>
        LoadAndRenderList<UserSummaryPayload>(
            "User",
            "Loading users from the instance...",
            _userApiClient.GetUsers,
            BuildUsersPage);

    private static ListPageModel BuildUsersPage(IReadOnlyList<UserSummaryPayload> users)
    {
        var page = new ListPageModel
        {
            Id = "config-users",
            Title = "Users",
            EntityTypeName = "User",
            Pagination = new PaginationState { TotalItems = users.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Username"));
        page.AddColumn(ListColumnDefinition.Create(ColumnEmail, "Email"));
        page.AddColumn(ListColumnDefinition.Create(ColumnRoles, "Roles"));
        page.AddColumn(ListColumnDefinition.Create(ColumnActive, "Active"));

        var lastLoginColumn = ListColumnDefinition.Create(ColumnLastLogin, "Last Login");
        lastLoginColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(lastLoginColumn);

        foreach (var user in users)
        {
            var row = new ListRowModel { Id = user.Username };
            row.SetValue(ColumnName, user.Username);
            row.SetValue(ColumnEmail, user.Email);
            row.SetValue(ColumnRoles, string.Join(", ", user.Roles));
            row.SetValue(ColumnActive, DescribeEnabled(user.IsActive));
            row.SetValue(
                ColumnLastLogin,
                user.LastLoginAt?.ToString("g", CultureInfo.CurrentCulture) ?? "Never");
            row.Status = DescribeEnabledStatus(user.IsActive);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's roles through the real API client and paints them through the
    /// render-agnostic seam.
    /// </summary>
    private Task ShowRolesList() =>
        LoadAndRenderList<RoleSummaryPayload>(
            "Role",
            "Loading roles from the instance...",
            _roleApiClient.GetRoles,
            BuildRolesPage);

    private static ListPageModel BuildRolesPage(IReadOnlyList<RoleSummaryPayload> roles)
    {
        var page = new ListPageModel
        {
            Id = "config-roles",
            Title = "Roles",
            EntityTypeName = "Role",
            Pagination = new PaginationState { TotalItems = roles.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDisplayName, "Display Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnScope, "Scope"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDescription, "Description"));

        foreach (var role in roles)
        {
            var row = new ListRowModel { Id = role.Name };
            row.SetValue(ColumnName, role.Name);
            row.SetValue(ColumnDisplayName, role.DisplayName);
            row.SetValue(ColumnScope, role.IsTenantScoped ? "Tenant" : "Global");
            row.SetValue(ColumnDescription, role.Description);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's tenants through the real API client and paints them through the
    /// render-agnostic seam.
    /// </summary>
    private Task ShowTenantsList() =>
        LoadAndRenderList<TenantSummaryPayload>(
            "Tenant",
            "Loading tenants from the instance...",
            ct => _tenantApiClient.GetTenants(includeInactive: true, ct),
            BuildTenantsPage);

    private static ListPageModel BuildTenantsPage(IReadOnlyList<TenantSummaryPayload> tenants)
    {
        var page = new ListPageModel
        {
            Id = "config-tenants",
            Title = "Tenants",
            EntityTypeName = "Tenant",
            Pagination = new PaginationState { TotalItems = tenants.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnSlug, "Slug"));
        page.AddColumn(ListColumnDefinition.Create(ColumnActive, "Active"));
        page.AddColumn(ListColumnDefinition.Create(ColumnRoles, "Available Roles"));

        foreach (var tenant in tenants)
        {
            var row = new ListRowModel { Id = tenant.Name };
            row.SetValue(ColumnName, tenant.Name);
            row.SetValue(ColumnSlug, tenant.Slug);
            row.SetValue(ColumnActive, DescribeEnabled(tenant.IsActive));
            row.SetValue(ColumnRoles, string.Join(", ", tenant.AvailableRoles));
            row.Status = DescribeEnabledStatus(tenant.IsActive);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's data-quality rules through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowQualityRulesList() =>
        LoadAndRenderList<QualityRuleSummaryPayload>(
            "Quality Rule",
            "Loading quality rules from the instance...",
            _qualityApiClient.GetRules,
            BuildQualityRulesPage);

    private static ListPageModel BuildQualityRulesPage(IReadOnlyList<QualityRuleSummaryPayload> rules)
    {
        var page = new ListPageModel
        {
            Id = "config-quality-rules",
            Title = "Quality Rules",
            EntityTypeName = "Quality Rule",
            Pagination = new PaginationState { TotalItems = rules.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDescription, "Description"));
        page.AddColumn(ListColumnDefinition.Create(ColumnEnabled, "Enabled"));

        foreach (var rule in rules)
        {
            var row = new ListRowModel { Id = rule.Name };
            row.SetValue(ColumnName, rule.Name);
            row.SetValue(ColumnDescription, rule.Description);
            row.SetValue(ColumnEnabled, DescribeEnabled(rule.IsEnabled));
            row.Status = DescribeEnabledStatus(rule.IsEnabled);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's ETL orchestration projects through the real API client and paints
    /// them through the render-agnostic seam.
    /// </summary>
    private Task ShowEtlProjectsList() =>
        LoadAndRenderList<ProjectConfiguration>(
            "ETL Project",
            "Loading ETL projects from the instance...",
            _projectApiClient.ListProjects,
            BuildEtlProjectsPage);

    private static ListPageModel BuildEtlProjectsPage(IReadOnlyList<ProjectConfiguration> projects)
    {
        var page = new ListPageModel
        {
            Id = "config-etl-projects",
            Title = "ETL Projects",
            EntityTypeName = "ETL Project",
            Pagination = new PaginationState { TotalItems = projects.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDescription, "Description"));
        page.AddColumn(ListColumnDefinition.Create(ColumnEnabled, "Enabled"));

        foreach (var project in projects)
        {
            var row = new ListRowModel { Id = project.Name };
            row.SetValue(ColumnName, project.Name);
            row.SetValue(ColumnDescription, project.Description ?? string.Empty);
            row.SetValue(ColumnEnabled, DescribeEnabled(project.IsEnabled));
            row.Status = DescribeEnabledStatus(project.IsEnabled);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches the instance's root orchestration nodes through the real API client and paints them
    /// through the render-agnostic seam.
    /// </summary>
    private Task ShowEtlNodesList() =>
        LoadAndRenderList<OrchestrationNodeConfiguration>(
            "ETL Node",
            "Loading root orchestration nodes from the instance...",
            _nodeApiClient.ListRootNodes,
            BuildEtlNodesPage);

    private static ListPageModel BuildEtlNodesPage(IReadOnlyList<OrchestrationNodeConfiguration> nodes)
    {
        var page = new ListPageModel
        {
            Id = "config-etl-nodes",
            Title = "ETL Nodes",
            EntityTypeName = "ETL Node",
            Pagination = new PaginationState { TotalItems = nodes.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnDescription, "Description"));

        var ordinalColumn = ListColumnDefinition.Create(ColumnOrdinal, "Ordinal");
        ordinalColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(ordinalColumn);

        page.AddColumn(ListColumnDefinition.Create(ColumnEnabled, "Enabled"));

        foreach (var node in nodes)
        {
            var row = new ListRowModel { Id = node.Name };
            row.SetValue(ColumnName, node.Name);
            row.SetValue(ColumnDescription, node.Description ?? string.Empty);
            row.SetValue(ColumnOrdinal, node.Ordinal.ToString(CultureInfo.CurrentCulture));
            row.SetValue(ColumnEnabled, DescribeEnabled(node.IsEnabled));
            row.Status = DescribeEnabledStatus(node.IsEnabled);

            page.AddRow(row);
        }

        return page;
    }

    /// <summary>
    /// Fetches a list from the given real API client and paints it through the render-agnostic seam.
    /// </summary>
    /// <remarks>
    /// Why shared: <see cref="ShowDataStoresList"/>, <see cref="ShowDataSetsList"/>,
    /// <see cref="ShowPipelinesList"/> and <see cref="ShowSecretManagersList"/> all follow the exact
    /// fetch/fail-loud/render shape <see cref="ShowConnectionsList"/> established — factored here so
    /// that shape isn't repeated four more times. The per-area page-building (columns, DTO fields)
    /// stays in its own <c>Build*Page</c> method since those genuinely differ per area.
    /// </remarks>
    private async Task LoadAndRenderList<T>(
        string entityTypeName,
        string loadingMessage,
        Func<CancellationToken, Task<IGenericResult<IReadOnlyList<T>>>> fetch,
        Func<IReadOnlyList<T>, ListPageModel> buildPage)
    {
        IReadOnlyList<T>? items = null;
        string? error = null;

        await Console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(Theme.Colors.Primary))
            .StartAsync(loadingMessage, async _ =>
            {
                var result = await fetch(default).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    items = result.Value;
                }
                else
                {
                    error = result.CurrentMessage;
                }
            }).ConfigureAwait(false);

        if (error is not null)
        {
            RenderStatus(error, isError: true);
            PauseForAcknowledgement();
            return;
        }

        if (items is null)
        {
            RenderStatus($"The instance reported success but returned no {entityTypeName} data.", isError: true);
            PauseForAcknowledgement();
            return;
        }

        var render = await _renderer.RenderListPage(buildPage(items), _renderContext).ConfigureAwait(false);
        if (!render.Success)
        {
            RenderStatus(render.Error ?? $"The {entityTypeName} list could not be rendered.", isError: true);
            PauseForAcknowledgement();
        }
    }

    private static string DescribeLastTest(bool? lastTestSuccess) => lastTestSuccess switch
    {
        true => "Passed",
        false => "Failed",
        null => "Never",
    };

    private void PauseForAcknowledgement()
    {
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Muted}]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }
}
