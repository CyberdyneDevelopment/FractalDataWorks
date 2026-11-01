# The notifications domain

Notifications and messaging: notification rules and channels, messaging, their endpoints and clients.

This folder, `public/src/notifications/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/notifications/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/notifications/`.

## Projects

fractaldataworks:

- `Fdw.Services.Messaging`
- `Fdw.Services.Messaging.Abstractions`
- `Fdw.Services.Messaging.Clients`: API client, DTOs, and registration types for message and message-access-request operations.
- `Fdw.Services.Messaging.Endpoints`: Generic endpoint base classes and DTOs for messaging operations via FastEndpoints.
- `Fdw.Services.Notifications`
- `Fdw.Services.Notifications.Abstractions`
- `Fdw.Services.Notifications.Clients`: API clients and registration types for notification, rule, and recipient-list operations.
- `Fdw.Services.Notifications.Endpoints`: Generic endpoint base classes and DTOs for notification operations via FastEndpoints.
- `Fdw.Services.Notifications.System`

reference-servicetypes:

- `FdwReferenceAlerts.Endpoints`: The alert HTTP endpoints: what an alert allows the caller, taking an action on it and taking the action back.
- `FdwReferenceInbox.Endpoints`: The inbox HTTP endpoints: a person's messages, to list, read, mark read, dismiss, archive, delete and send.
- `FdwReferenceMessaging.Endpoints`: The Messaging domain HTTP endpoints - messages, notifications, rules and preferences.
- `FdwReferenceNotifications.Console`: Console notification service — the aggregation over FDW's Notifications machinery, plus its service type registration.
- `FdwReferenceNotifications.Console.Registration`: The Console notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceNotifications.Email`: Email notification service — the aggregation over FDW's Notifications machinery, plus its service type registration.
- `FdwReferenceNotifications.Email.Registration`: The Email notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceNotifications.System`: System notification service — the aggregation over FDW's Notifications machinery, plus its service type registration.
- `FdwReferenceNotifications.System.Registration`: The System notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceNotifications.Teams`: Microsoft Teams notification service — the aggregation over FDW's Notifications machinery, plus its service type registration.
- `FdwReferenceNotifications.Teams.Registration`: The Teams notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceNotifications.Webhook`: Webhook notification service — the aggregation over FDW's Notifications machinery, plus its service type registration.
- `FdwReferenceNotifications.Webhook.Registration`: The Webhook notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceUiFeedback.Endpoints`: The UI comment HTTP endpoints: leave, read, edit, agree with, comment on, acknowledge, resolve and delete a comment.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `INotificationRequest` (Fdw.Services.Notifications.Abstractions)
- `IMessageService` (Fdw.Services.Messaging.Abstractions)
- `IUiFeedbackService` (Fdw.Services.Messaging.Abstractions)
- `IAlertAction` (Fdw.Services.Messaging.Abstractions)
- `IAlertSource` (Fdw.Services.Messaging.Abstractions)
- `INotificationImplementationConfiguration` (Fdw.Services.Notifications.Abstractions)
- `IAlertActionService` (Fdw.Services.Messaging.Abstractions)
- `INotificationResult` (Fdw.Services.Notifications.Abstractions)
- `IRuleNotifier` (Fdw.Services.Notifications.Abstractions)
- `IMessagingConfigurationProvider` (Fdw.Services.Messaging.Abstractions)
- `IPlatformNotification` (Fdw.Services.Notifications.Abstractions)
- `IAccessRequestService` (Fdw.Services.Messaging.Abstractions)
- `INotificationServiceProvider` (Fdw.Services.Notifications.Abstractions)
- `NotificationConditionTypeBase` (Fdw.Services.Notifications.Abstractions)
- `NotificationStatusBase` (Fdw.Services.Notifications.Abstractions)
- `AlertActionBase` (Fdw.Services.Messaging.Abstractions)
- `IMessagingImplementationConfiguration` (Fdw.Services.Messaging.Abstractions)
- `INotificationConfigurationProvider` (Fdw.Services.Notifications.Abstractions)
- `INotificationType` (Fdw.Services.Notifications.Abstractions)
- `NotificationChannelBase` (Fdw.Services.Notifications.Abstractions)
- and 17 more in the `.Abstractions` projects above

## Rules that matter here

- No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/notifications/Fdw.Services.Messaging.Tests/Fdw.Services.Messaging.Tests.csproj -c Release`
- `dotnet test public/tests/notifications/Fdw.Services.Notifications.Abstractions.Tests/Fdw.Services.Notifications.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/notifications/Fdw.Services.Notifications.Tests/Fdw.Services.Notifications.Tests.csproj -c Release`
- `dotnet test public/tests/notifications/FdwReferenceMessaging.Endpoints.Tests/FdwReferenceMessaging.Endpoints.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/notifications/FdwReferenceNotifications.Console.Tests/FdwReferenceNotifications.Console.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/notifications/FdwReferenceNotifications.Teams.Tests/FdwReferenceNotifications.Teams.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/notifications/FdwReferenceNotifications.Webhook.Tests/FdwReferenceNotifications.Webhook.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `messagelogging` (12), `collections` (9), `data` (9), `results` (7), `configuration` (6), `services` (6), `commands` (4), `endpoints` (4), `authentication` (3), `abstractions` (2), `connections` (2), `authorization` (1), `hosts` (1), `pipelines` (1), `validation` (1).
Depended on by: `pipelines` (4), `resiliency` (1), `shells` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-notify`, `catalog-msg`.
