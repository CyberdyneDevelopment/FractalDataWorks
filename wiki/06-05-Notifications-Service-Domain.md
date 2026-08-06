# Notifications Service Domain

The Notifications domain delivers alerts and events through pluggable channels. It follows the standard service domain pattern with `NotificationTypes` as the `ServiceTypeCollection` for implementation packages, and a `NotificationChannels` `TypeCollection` (in `.Abstractions`) that declares the five channel descriptors.

## Channels

Five channels are declared in the `NotificationChannels` collection; four have real, deployable `INotificationService` implementations that send live traffic, and Console is the fifth.

| Channel | Where it registers | Purpose |
|---------|--------------------|---------|
| `Webhook` | `.Notifications.Webhook` | generic configurable HTTP POST/PUT via `IHttpClientFactory` |
| `Email` | `.Notifications.Email` | SMTP via MailKit/MimeKit (StartTls, importance) |
| `System` | `.Notifications.System` | persisted in-app message pushed live over SignalR (`/hubs/messages` → `NotificationBell`) |
| `Teams` | core `.Notifications` (`Services/TeamsNotificationService`) | Teams incoming-webhook MessageCard/AdaptiveCard via `IHttpClientFactory` |
| `Console` | `.Notifications.Console` | structured log output (dev/test) |

`NotificationDispatcher` is the single fan-out seam: it routes an `INotificationRequest` to the `INotificationService` whose `Channel.Name` matches `request.ChannelName` — deterministic name-based routing, no per-channel branching.

> **Not built:** SMS, Slack, PagerDuty, native OS toast, web-push, and desktop system-tray transports do not exist anywhere in the ecosystem. The browser-native substitute for a tray toast is the in-app SignalR `MessageHub`/`NotificationBell` push. A new sending channel is one `[ServiceTypeOption]`/`INotificationService` subclass.

## Package Structure

```
Services.Notifications.Abstractions/  # Interfaces, base classes, NotificationChannels descriptors, builder
Services.Notifications/              # NotificationTypes, NotificationTypeBase, NotificationDispatcher, TeamsNotificationService
Services.Notifications.Webhook/      # WebhookNotificationType + factory
Services.Notifications.Email/        # EmailNotificationType + factory (MailKit)
Services.Notifications.System/       # SystemNotificationType + factory (SignalR)
Services.Notifications.Console/      # ConsoleNotificationType + factory
Services.Notifications.Endpoints/    # REST endpoints
```

## Registration

Each notification implementation package contains a `[ServiceTypeOption(typeof(NotificationTypes), "<name>")]` class. With `Fdw.Registration.SourceGenerators` in the entry-point app, the emitted `[ModuleInitializer]` registers every referenced `[ServiceTypeOption]` at assembly load — **adding the package reference IS the registration intent**.

`NotificationTypes` is an ordinary `[ServiceTypeCollection]`, so its three-phase methods run inside
the single `PlatformServices.Configure`/`Register`/`Initialize` sweep — there is no
hand-written `NotificationTypes.Configure(...)` call in `Program.cs`:

```csharp
// Phase 1 — Configure + Register (before builder.Build()) — one sweep, all domains.
PlatformServices.Configure(builder, loggerFactory);
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Phase 3 — Initialize (after Build) — one sweep, dependency-safe Group order.
PlatformServices.Initialize(app.Services, loggerFactory);
```

See [20-02 Service Startup Order](20-02-Service-Startup-Order.md) for the full Program.cs shape across all service domains.

## Configuration

### Webhook

```json
{
  "Notifications": {
    "Webhook": [
      {
        "Name": "OrderAlerts",
        "IsEnabled": true,
        "Url": "https://hooks.example.com/notify",
        "Method": "POST",
        "ContentType": "application/json",
        "RetryCount": 3,
        "TimeoutSeconds": 30,
        "PayloadTemplate": null
      }
    ]
  }
}
```

**`WebhookNotificationConfiguration` properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `Url` | — | Target URL (required) |
| `Method` | `POST` | HTTP method |
| `ContentType` | `application/json` | Request Content-Type |
| `PayloadTemplate` | `null` | Custom template (see below) |
| `RetryCount` | `3` | Retry attempts on transient failures |
| `TimeoutSeconds` | `30` | Request timeout |

### Console

```json
{
  "Notifications": {
    "Console": [
      {
        "Name": "DevAlerts",
        "IsEnabled": true,
        "LogLevel": "Information"
      }
    ]
  }
}
```

**`ConsoleNotificationConfiguration` properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `LogLevel` | `Information` | Serilog log level for emitted messages |

## PayloadTemplate Token System

When `PayloadTemplate` is set, the webhook factory replaces tokens before sending:

| Token | Replaced With |
|-------|---------------|
| `{subject}` | `NotificationRequest.Subject` |
| `{body}` | `NotificationRequest.Message` |
| `{type}` | Notification type name |
| `{timestamp}` | ISO 8601 UTC timestamp |

When `PayloadTemplate` is `null`, the factory sends a standard JSON payload:

```json
{
  "subject": "...",
  "body": "...",
  "type": "Webhook",
  "timestamp": "2026-02-21T12:00:00Z"
}
```

**Example template (Slack-compatible):**

```json
{
  "PayloadTemplate": "{\"text\": \"{subject}: {body}\"}"
}
```

## Sending Notifications

Inject `IFdwServiceProvider<IGenericNotification, INotificationConfiguration>` and use the builder:

```csharp
public class OrderService(IFdwServiceProvider<IGenericNotification, INotificationConfiguration> notifications)
{
    public async Task NotifyOrderCompleted(string orderId, CancellationToken ct)
    {
        var request = new NotificationRequestBuilder("Webhook")
            .WithSubject("Order Completed")
            .WithMessage($"Order {orderId} has been fulfilled.")
            .WithPriority(NotificationPriority.Normal)
            .WithCorrelationId(orderId)
            .Build();

        var service = notifications.Get("OrderAlerts");
        var result = await service.Send(request, ct);
    }
}
```

## Adding a New Channel

1. Create a `ServiceTypeOption` extending `NotificationTypeBase<,,>`:

```csharp
[ServiceTypeOption(typeof(NotificationTypes), "Teams")]
public sealed class TeamsNotificationType
    : NotificationTypeBase<INotificationService, ITeamsNotificationFactory, TeamsNotificationConfiguration>
{
    public TeamsNotificationType()
        : base("Teams", NotificationChannels.ByName("Teams"),
               "Microsoft Teams Notifications",
               "Send adaptive cards to Teams channels")
    { }

    public override IServiceCollection RegisterRequiredServices(IServiceCollection services)
    {
        services.AddSingleton<ITeamsNotificationFactory, TeamsNotificationFactory>();
        return services;
    }

    public override void RegisterFactory(IFdwServiceProvider<IGenericNotification, INotificationConfiguration> provider, IServiceProvider services)
    {
        var factory = services.GetRequiredService<ITeamsNotificationFactory>();
        provider.Register(Name, factory);

        // Configuration is provided by the domain's DefaultConfigurationProvider<T>
        // which holds the per-domain ConfigurationProvider cache internally
    }

    public override void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<List<TeamsNotificationConfiguration>>(
            configuration.GetSection("Notifications:Teams"));
    }
}
```

2. Create a `[ManagedConfiguration]` class and factory, following the Webhook pattern.

## See Also

- [Service Domains Overview](06-01-Service-Domains-Overview.md)
- [Creating a Service Domain](06-02-Creating-Service-Domain.md)
- [Transformations Service Domain](06-04-Transformations-Service-Domain.md)
